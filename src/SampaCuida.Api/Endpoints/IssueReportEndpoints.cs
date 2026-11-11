using Microsoft.AspNetCore.Http.HttpResults;
using SampaCuida.Api.Validation;
using SampaCuida.Application.Common.Paging;
using SampaCuida.Application.IssueReports;

namespace SampaCuida.Api.Endpoints;

public static class IssueReportEndpoints
{
    public static IEndpointRouteBuilder MapIssueReportEndpoints(this IEndpointRouteBuilder app)
    {
        // Rotas do gestor da prefeitura (protegidas por JWT na fase seguinte).
        var group = app.MapGroup("/api/issue-reports").WithTags("Ocorrências (gestor)");

        group.MapGet("/", List)
            .WithSummary("Lista as ocorrências com indicadores calculados (padrão: maior prioridade primeiro)")
            .WithValidation<IssueReportQuery>();

        group.MapGet("/{id:int}", GetById)
            .WithSummary("Detalhe da ocorrência com histórico de status e atribuições")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:int}/status", ChangeStatus)
            .WithSummary("Muda o status (IN_PROGRESS, RESOLVED ou REJECTED, este com motivo obrigatório)")
            .WithValidation<ChangeIssueStatusRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:int}/assignments", Assign)
            .WithSummary("Atribui uma equipe ativa (OPEN) ou troca a equipe (ASSIGNED)")
            .WithValidation<CreateAssignmentRequest>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Rota do cidadão: só leitura, sem dados internos e sem as ocorrências rejeitadas.
        app.MapGet("/api/public/issue-reports", ListPublic)
            .WithTags("Público (cidadão)")
            .WithSummary("Ocorrências para o cidadão acompanhar (sem dados internos; rejeitadas não aparecem)")
            .WithValidation<PublicIssueReportQuery>();

        return app;
    }

    private static async Task<Ok<PagedResponse<IssueReportSummaryResponse>>> List(
        [AsParameters] IssueReportQuery query, IIssueReportService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(query, cancellationToken));

    private static async Task<Ok<IssueReportDetailResponse>> GetById(
        int id, IIssueReportService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetByIdAsync(id, cancellationToken));

    private static async Task<Ok<IssueReportDetailResponse>> ChangeStatus(
        int id, ChangeIssueStatusRequest request, IIssueReportService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ChangeStatusAsync(id, request, cancellationToken));

    private static async Task<Created<IssueReportDetailResponse>> Assign(
        int id, CreateAssignmentRequest request, IIssueReportService service, CancellationToken cancellationToken)
    {
        var report = await service.AssignAsync(id, request, cancellationToken);
        return TypedResults.Created($"/api/issue-reports/{id}", report);
    }

    private static async Task<Ok<PagedResponse<PublicIssueReportResponse>>> ListPublic(
        [AsParameters] PublicIssueReportQuery query, IIssueReportService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListPublicAsync(query, cancellationToken));
}
