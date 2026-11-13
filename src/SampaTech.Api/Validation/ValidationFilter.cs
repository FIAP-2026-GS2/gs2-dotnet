using FluentValidation;

namespace SampaTech.Api.Validation;

/// <summary>
/// Roda o validador do FluentValidation antes da rota. Se houver erro, responde 400
/// com todos os campos inválidos de uma vez, e a rota nem é executada.
/// </summary>
public class ValidationFilter<T> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        var argument = context.Arguments.OfType<T>().FirstOrDefault();

        if (validator is null || argument is null)
            return await next(context);

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);

        if (!result.IsValid)
            return TypedResults.ValidationProblem(result.ToDictionary(), title: "Dados inválidos");

        return await next(context);
    }
}

public static class ValidationFilterExtensions
{
    // Uso: .WithValidation<MeuRequest>() — também documenta o 400 no Swagger.
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) =>
        builder
            .AddEndpointFilter<ValidationFilter<T>>()
            .ProducesValidationProblem();
}
