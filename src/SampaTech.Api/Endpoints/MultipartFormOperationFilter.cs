using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SampaTech.Api.Endpoints;

/// <summary>
/// O Swagger descreve um formulário [FromForm] como um campo só ("form") com o objeto dentro.
/// Assim o Swagger UI não mostra o botão de escolher arquivo. Este filtro "abre" o objeto:
/// cada campo (decision, notes, latitude, longitude, photo) vira um campo do formulário.
/// </summary>
public class MultipartFormOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content.TryGetValue("multipart/form-data", out var media) != true)
            return;

        var schema = media!.Schema;
        if (schema?.Properties.Count != 1 || schema.Properties.Values.Single().Reference is not { } reference)
            return;

        if (!context.SchemaRepository.Schemas.TryGetValue(reference.Id, out var form))
            return;

        media.Schema = new OpenApiSchema
        {
            Type = "object",
            Properties = form.Properties,
            Required = form.Required
        };
        media.Encoding.Clear();
    }
}
