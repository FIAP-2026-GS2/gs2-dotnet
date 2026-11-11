using FluentValidation;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Domain.Entities;
using SampaCuida.Domain.Enums;

namespace SampaCuida.Application.Captures;

public class CaptureImageRequestValidator : AbstractValidator<CaptureImageRequest>
{
    public CaptureImageRequestValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(Image.FileNameMaxLength).WithName("nome do arquivo");
        RuleFor(x => x.FilePath).NotEmpty().MaximumLength(Image.FilePathMaxLength)
            .Must(path => Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .WithMessage("O caminho do arquivo deve ser uma URL https (ex.: a URL do Blob Storage).");
        RuleFor(x => x.FileHash).Length(Image.FileHashLength).Matches("^[0-9a-fA-F]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.FileHash))
            .WithMessage("O hash do arquivo deve ser um SHA-256 com 64 caracteres hexadecimais.");
        // Privacidade: rostos e placas precisam ter sido borrados antes do envio.
        RuleFor(x => x.IsAnonymized).Equal(true)
            .WithMessage("A imagem precisa estar anonimizada (rostos e placas borrados).");
    }
}

public class CreateCaptureRequestValidator : AbstractValidator<CreateCaptureRequest>
{
    public CreateCaptureRequestValidator()
    {
        RuleFor(x => x.Source).NotNull().IsInEnum().WithName("origem");
        RuleFor(x => x.CaptureDate).NotNull().WithName("data da captura");
        RuleFor(x => x.Image).NotNull().WithName("imagem").SetValidator(new CaptureImageRequestValidator());

        When(x => x.Source == CaptureSource.RECORDING_FRAME, () =>
        {
            RuleFor(x => x.CameraId).NotNull().GreaterThan(0).WithName("câmera");
            RuleFor(x => x.CaptureScheduleId).NotNull().GreaterThan(0).WithName("agenda");
        });

        When(x => x.Source == CaptureSource.EVENT, () =>
        {
            RuleFor(x => x.CameraId).NotNull().GreaterThan(0).WithName("câmera");
            RuleFor(x => x.TriggerEventId).NotNull().GreaterThan(0).WithName("alerta");
        });

        When(x => x.Source == CaptureSource.CITIZEN, () =>
        {
            RuleFor(x => x.UserId).NotEmpty().MaximumLength(Capture.UserIdMaxLength).WithName("usuário");
            RuleFor(x => x.Latitude).NotNull().InclusiveBetween(-90, 90).WithName("latitude");
            RuleFor(x => x.Longitude).NotNull().InclusiveBetween(-180, 180).WithName("longitude");
        });
    }
}

public class CaptureQueryValidator : AbstractValidator<CaptureQuery>
{
    public CaptureQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithName("página");
        RuleFor(x => x.PageSize).InclusiveBetween(1, PagingExtensions.MaxPageSize).WithName("tamanho da página");
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("O fim do período não pode ser anterior ao início.");
    }
}
