using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SampaCuida.Api.RateLimiting;

/// <summary>Documenta o 429 no Swagger em todas as rotas que têm limite de requisições.</summary>
public class TooManyRequestsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var withoutLimit = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<DisableRateLimitingAttribute>()
            .Any();

        if (!withoutLimit)
            operation.Responses.TryAdd("429", new OpenApiResponse
            {
                Description = "Limite de requisições atingido (veja o header Retry-After)"
            });
    }
}
