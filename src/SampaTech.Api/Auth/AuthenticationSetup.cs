using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SampaTech.Application.Auth;
using SampaTech.Infrastructure.Auth;

namespace SampaTech.Api.Auth;

public static class AuthenticationSetup
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        var java = configuration.GetSection(JavaAuthOptions.SectionName).Get<JavaAuthOptions>() ?? new JavaAuthOptions();

        services
            // Entrada única: cada token vai para um validador só (o da .NET ou o da API Java).
            .AddAuthentication(JavaPortalToken.SelectorScheme)
            .AddPolicyScheme(JavaPortalToken.SelectorScheme, "Token da .NET ou da API Java", options =>
                options.ForwardDefaultSelector = context => JavaPortalToken.LooksLikeJavaToken(context)
                    ? JavaPortalToken.Scheme
                    : JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Mantém os nomes do token ("sub", "role") sem a tradução antiga da Microsoft.
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuers = [jwt.Issuer, .. jwt.AdditionalValidIssuers],
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    // Falha logo na subida se a chave não estiver configurada.
                    IssuerSigningKey = JwtAuthService.SigningKey(jwt),
                    // Tolerância pequena para diferença de relógio entre servidores.
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };

                options.Events = ProblemDetailsEvents();
            })
            // Token do app do cidadão, emitido pela API Java: sem emissor e sem público, assinado com o
            // JWT_SECRET dela (texto puro, HS256/384/512). Só o papel CITIZEN passa.
            .AddJwtBearer(JavaPortalToken.Scheme, options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    // Sem o segredo configurado, uma chave aleatória: nenhum token da Java passa, e a API sobe normal.
                    IssuerSigningKey = new SymmetricSecurityKey(java.Secret.Length >= JwtOptions.MinKeyLength
                        ? Encoding.UTF8.GetBytes(java.Secret)
                        : RandomNumberGenerator.GetBytes(64)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256, SecurityAlgorithms.HmacSha384, SecurityAlgorithms.HmacSha512],
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };

                var events = ProblemDetailsEvents();
                events.OnTokenValidated = JavaPortalToken.OnTokenValidated;
                options.Events = events;
            });

        services.AddAuthorization(options =>
        {
            // Proteção por padrão: toda rota exige login, a não ser que seja marcada como anônima.
            // Como uma porta que tranca sozinha: esquecer de proteger uma rota nova não a deixa aberta.
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

            // Escada: cada política aceita o seu nível e os de cima.
            options.AddPolicy(AuthPolicies.Read, policy => policy.RequireRole(Roles.Admin, Roles.Manager, Roles.Viewer));
            options.AddPolicy(AuthPolicies.Manage, policy => policy.RequireRole(Roles.Admin, Roles.Manager));
            options.AddPolicy(AuthPolicies.Delete, policy => policy.RequireRole(Roles.Admin));
            options.AddPolicy(AuthPolicies.IaService, policy => policy.RequireRole(Roles.IaService));

            // Cidadão: só envia e acompanha os próprios relatos com foto.
            options.AddPolicy(AuthPolicies.Citizen, policy => policy.RequireRole(Roles.Citizen));
            options.AddPolicy(AuthPolicies.ReadOrCitizen,
                policy => policy.RequireRole(Roles.Admin, Roles.Manager, Roles.Viewer, Roles.Citizen));
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        return services;
    }

    // 401 e 403 no mesmo formato (ProblemDetails) e em português, como os demais erros.
    private static JwtBearerEvents ProblemDetailsEvents() => new()
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.Headers.WWWAuthenticate = "Bearer";
            await WriteProblemAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                "Não autenticado",
                "Envie um token válido no header Authorization: Bearer <token> (obtido em POST /api/auth/login).");
        },
        OnForbidden = context => WriteProblemAsync(context.HttpContext, StatusCodes.Status403Forbidden,
            "Acesso negado",
            HttpMethods.IsDelete(context.HttpContext.Request.Method)
                ? "Só o ADMIN pode apagar registros. Para tirar de uso, desative (PATCH .../deactivate)."
                : "O seu papel não tem permissão para esta rota.")
    };

    private static Task WriteProblemAsync(HttpContext httpContext, int status, string title, string detail)
    {
        httpContext.Response.StatusCode = status;
        var problemDetails = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();

        return problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        }).AsTask();
    }
}

/// <summary>Lê o usuário ("sub") do token da requisição atual.</summary>
public class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? UserId => accessor.HttpContext?.User.FindFirst("sub")?.Value;

    public bool IsInRole(string role) => accessor.HttpContext?.User.IsInRole(role) ?? false;
}
