using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SampaTech.Api.RateLimiting;

/// <summary>Nomes das políticas usadas nas rotas.</summary>
public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string IaService = "ia-service";
    public const string Public = "public";
}

/// <summary>Configuração "RateLimiting" do appsettings (dá para ajustar no Azure sem recompilar).</summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public WindowLimit Login { get; set; } = new() { PermitLimit = 5, WindowSeconds = 60 };
    public BucketLimit IaService { get; set; } = new() { TokenLimit = 100, TokensPerPeriod = 50, ReplenishmentSeconds = 60 };
    public WindowLimit Public { get; set; } = new() { PermitLimit = 60, WindowSeconds = 60, Segments = 6 };
    public WindowLimit Default { get; set; } = new() { PermitLimit = 120, WindowSeconds = 60, Segments = 6 };
}

public class WindowLimit
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
    public int Segments { get; set; } = 1; // usado só pela janela deslizante
}

public class BucketLimit
{
    public int TokenLimit { get; set; }
    public int TokensPerPeriod { get; set; }
    public int ReplenishmentSeconds { get; set; }
}

public static class RateLimitingSetup
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var limits = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
                     ?? new RateLimitingOptions();

        // No Azure, a requisição passa por um balanceador: o IP do cliente vem no cabeçalho X-Forwarded-For.
        // ForwardLimit = 1 (padrão): só o último salto é considerado, o que o balanceador acrescentou.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services.AddRateLimiter(options =>
        {
            // Passou do limite: 429 na hora, sem fila.
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRejectedAsync;

            // Login: janela fixa e rígida, por IP (ainda não se sabe quem é). Contra adivinhar senha.
            options.AddPolicy(RateLimitPolicies.Login, context =>
                RateLimitPartition.GetFixedWindowLimiter(ByIp(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.Login.PermitLimit,
                    Window = TimeSpan.FromSeconds(limits.Login.WindowSeconds),
                    QueueLimit = 0
                }));

            // Serviço de IA: balde de fichas. Aceita a rajada de um dia de chuva e segura o ritmo médio.
            options.AddPolicy(RateLimitPolicies.IaService, context =>
                RateLimitPartition.GetTokenBucketLimiter(ByClient(context), _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = limits.IaService.TokenLimit,
                    TokensPerPeriod = limits.IaService.TokensPerPeriod,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(limits.IaService.ReplenishmentSeconds),
                    QueueLimit = 0
                }));

            // Rota pública (internet, sem login): janela deslizante por IP, sem brecha na virada do minuto.
            options.AddPolicy(RateLimitPolicies.Public, context =>
                RateLimitPartition.GetSlidingWindowLimiter(ByIp(context), _ => SlidingWindow(limits.Public)));

            // Todas as demais rotas: janela deslizante por usuário (ou por IP, sem login).
            // Rotas com política própria (ou sem limite, como o /health) ficam de fora deste limite geral.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var metadata = context.GetEndpoint()?.Metadata;

                if (metadata?.GetMetadata<EnableRateLimitingAttribute>() is not null
                    || metadata?.GetMetadata<DisableRateLimitingAttribute>() is not null)
                    return RateLimitPartition.GetNoLimiter("rota-com-politica-propria");

                return RateLimitPartition.GetSlidingWindowLimiter(ByClient(context), _ => SlidingWindow(limits.Default));
            });
        });

        return services;
    }

    private static SlidingWindowRateLimiterOptions SlidingWindow(WindowLimit limit) => new()
    {
        PermitLimit = limit.PermitLimit,
        Window = TimeSpan.FromSeconds(limit.WindowSeconds),
        SegmentsPerWindow = limit.Segments,
        QueueLimit = 0
    };

    // Com login, a cota é de cada usuário (várias pessoas na mesma rede não dividem a cota).
    // Sem login, a cota é do IP.
    private static string ByClient(HttpContext context) =>
        context.User.FindFirst("sub")?.Value is { } user ? $"usuario:{user}" : ByIp(context);

    private static string ByIp(HttpContext context) =>
        $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido"}";

    // 429 no mesmo formato dos demais erros, dizendo quando tentar de novo (header Retry-After).
    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var http = context.HttpContext;
        var detail = "Limite de requisições atingido. Aguarde um pouco e tente de novo.";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
            http.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
            detail = $"Limite de requisições atingido. Tente de novo em {seconds} segundo(s).";
        }

        var problemDetails = http.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = new ProblemDetails
            {
                // Mesmo formato dos demais erros: o 429 é definido na RFC 6585.
                Type = "https://tools.ietf.org/html/rfc6585#section-4",
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Muitas requisições",
                Detail = detail
            }
        });
    }
}
