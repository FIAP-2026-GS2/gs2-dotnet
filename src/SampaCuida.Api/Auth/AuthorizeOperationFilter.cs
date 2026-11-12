using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SampaCuida.Api.Auth;

/// <summary>
/// No Swagger, mostra o cadeado e as respostas 401/403 só nas rotas protegidas
/// (as anônimas, como o login e a rota pública, ficam sem cadeado).
/// </summary>
public class AuthorizeOperationFilter : IOperationFilter
{
    public const string SchemeName = "Bearer";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        if (metadata.OfType<IAllowAnonymous>().Any() || !metadata.OfType<IAuthorizeData>().Any())
            return;

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = SchemeName }
                }] = []
            }
        ];

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Sem token ou token inválido" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "O papel do usuário não tem permissão" });
    }
}
