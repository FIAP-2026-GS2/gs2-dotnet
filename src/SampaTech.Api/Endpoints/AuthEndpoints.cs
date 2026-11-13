using SampaTech.Api.RateLimiting;
using Microsoft.AspNetCore.Http.HttpResults;
using SampaTech.Api.Validation;
using SampaTech.Application.Auth;

namespace SampaTech.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", Login)
            .WithTags("Autenticação")
            .WithSummary("Faz login e devolve o token JWT (válido por 60 minutos)")
            .WithValidation<LoginRequest>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Login);

        return app;
    }

    private static Ok<LoginResponse> Login(LoginRequest request, IAuthService authService) =>
        TypedResults.Ok(authService.Login(request));
}
