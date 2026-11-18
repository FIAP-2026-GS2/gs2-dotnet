using System.Globalization;
using System.Security.Cryptography;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SampaTech.Api.Idempotency;
using SampaTech.Api.Validation;
using SampaTech.Application.Auth;
using SampaTech.Application.CitizenFeedbacks;
using SampaTech.Application.Common.Exceptions;
using SampaTech.Application.Common.Paging;
using SampaTech.Application.Common.Photos;
using SampaTech.Domain.Enums;

namespace SampaTech.Api.Endpoints;

public static class CitizenFeedbackEndpoints
{
    public static IEndpointRouteBuilder MapCitizenFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        // Cidadão: manda a foto de uma ocorrência resolvida, confirmando ou contestando o conserto.
        app.MapPost("/api/issue-reports/{id:int}/citizen-feedback", Submit)
            .WithTags("Relatos do cidadão")
            .WithSummary("Cidadão confirma ou contesta, com foto, o conserto de uma ocorrência resolvida " +
                         "(multipart: decision, notes, latitude, longitude e photo JPG/PNG até 5 MB; exige o header Idempotency-Key)")
            .Accepts<CitizenFeedbackForm>("multipart/form-data")
            .DisableAntiforgery()
            .WithIdempotency<CitizenFeedbackForm>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status413PayloadTooLarge)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization(AuthPolicies.Citizen);

        var group = app.MapGroup("/api/citizen-feedback").WithTags("Relatos do cidadão");

        // Equipe: fila de relatos para revisar (padrão: mais antigos primeiro).
        group.MapGet("/", List)
            .WithSummary("Lista os relatos dos cidadãos (filtro por status e ocorrência; use status=PENDING para a fila de revisão)")
            .WithValidation<CitizenFeedbackQuery>()
            .RequireAuthorization(AuthPolicies.Read);

        // Cidadão acompanha o próprio relato; a equipe vê qualquer um.
        group.MapGet("/{id:int}", GetById)
            .WithSummary("Busca um relato com a mensagem para o cidadão (o cidadão só vê os próprios)")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthPolicies.ReadOrCitizen);

        // A foto borrada, para a equipe conferir antes de aprovar (o cidadão vê só as próprias).
        group.MapGet("/{id:int}/photo", GetPhoto)
            .WithSummary("Mostra a foto (já borrada) do relato; a equipe usa para decidir a aprovação")
            .Produces(StatusCodes.Status200OK, contentType: "image/jpeg")
            .Produces(StatusCodes.Status200OK, contentType: "image/png")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthPolicies.ReadOrCitizen);

        // Equipe: aprova ou recusa a foto. Contestação aprovada reabre a ocorrência.
        group.MapPatch("/{id:int}/review", Review)
            .WithSummary("Aprova ou recusa o relato (recusar exige motivo; contestação aprovada reabre a ocorrência)")
            .WithValidation<ReviewCitizenFeedbackRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthPolicies.Manage);

        return app;
    }

    private static async Task<Results<Created<CitizenFeedbackResponse>, ValidationProblem>> Submit(
        int id,
        [FromForm] CitizenFeedbackForm form,
        IValidator<SubmitCitizenFeedbackRequest> validator,
        IOptions<CitizenPhotoOptions> options,
        ICitizenFeedbackService service,
        CancellationToken cancellationToken)
    {
        var request = form.ToRequest();
        var validation = await validator.ValidateAsync(request, cancellationToken);

        if (form.Photo is null || form.Photo.Length == 0)
            validation.Errors.Add(new FluentValidation.Results.ValidationFailure("photo", "Envie a foto (JPG ou PNG)."));

        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary(), title: "Dados inválidos");

        // Confere o tamanho antes de ler o arquivo para a memória.
        var maxBytes = options.Value.MaxBytes;
        if (form.Photo!.Length > maxBytes)
            throw new PayloadTooLargeException($"A foto deve ter até {maxBytes / (1024 * 1024)} MB.");

        using var memory = new MemoryStream();
        await form.Photo.CopyToAsync(memory, cancellationToken);
        var photo = new PhotoUpload(form.Photo.FileName, memory.ToArray());

        var feedback = await service.SubmitAsync(id, request, photo, cancellationToken);
        return TypedResults.Created($"/api/citizen-feedback/{feedback.Id}", feedback);
    }

    private static async Task<Ok<PagedResponse<CitizenFeedbackResponse>>> List(
        [AsParameters] CitizenFeedbackQuery query, ICitizenFeedbackService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(query, cancellationToken));

    private static async Task<Ok<CitizenFeedbackResponse>> GetById(
        int id, ICitizenFeedbackService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetByIdAsync(id, cancellationToken));

    private static async Task<FileContentHttpResult> GetPhoto(
        int id, ICitizenFeedbackService service, CancellationToken cancellationToken)
    {
        var photo = await service.GetPhotoAsync(id, cancellationToken);
        return TypedResults.File(photo.Content, photo.ContentType);
    }

    private static async Task<Ok<CitizenFeedbackResponse>> Review(
        int id, ReviewCitizenFeedbackRequest request, ICitizenFeedbackService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ReviewAsync(id, request, cancellationToken));
}

/// <summary>
/// Formulário multipart enviado pelo app. Os nomes dos campos são os que o app manda:
/// decision, notes, latitude, longitude e photo.
/// </summary>
public class CitizenFeedbackForm : IIdempotencyFingerprint
{
    public CitizenFeedbackDecision? Decision { get; set; }
    public string? Notes { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public IFormFile? Photo { get; set; }

    public SubmitCitizenFeedbackRequest ToRequest() => new(Decision, Notes, Latitude, Longitude);

    // Mesma chave + mesmos campos + mesma foto = repetição; qualquer diferença = outro conteúdo (422).
    public string GetFingerprint()
    {
        var photoHash = "sem-foto";
        if (Photo is { Length: > 0 })
        {
            using var stream = Photo.OpenReadStream();
            photoHash = Convert.ToHexString(SHA256.HashData(stream));
        }

        return string.Join('|', Decision, Notes,
            Latitude?.ToString(CultureInfo.InvariantCulture), Longitude?.ToString(CultureInfo.InvariantCulture),
            photoHash);
    }
}
