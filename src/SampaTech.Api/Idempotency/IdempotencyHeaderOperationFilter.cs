using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SampaTech.Api.Idempotency;

/// <summary>Mostra o header Idempotency-Key (obrigatório) no Swagger das rotas idempotentes.</summary>
public class IdempotencyHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isIdempotent = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<RequiresIdempotencyKeyMetadata>()
            .Any();

        if (!isIdempotent)
            return;

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Identificador único do envio, com até 100 caracteres (recomendado: um UUID). " +
                          "Repita o mesmo valor nas novas tentativas.",
            // Sem "format: uuid": a API aceita qualquer texto, e o Swagger UI bloquearia valores como "teste-1".
            Schema = new OpenApiSchema { Type = "string", MaxLength = 100 }
        });
    }
}
