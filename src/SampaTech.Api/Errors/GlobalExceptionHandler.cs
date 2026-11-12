using Microsoft.AspNetCore.Diagnostics;
using SampaTech.Application.Common.Exceptions;
using SampaTech.Domain.Exceptions;

namespace SampaTech.Api.Errors;

/// <summary>
/// Um único lugar que transforma exceções em respostas HTTP padronizadas (ProblemDetails).
/// Por isso as rotas não têm try/catch.
/// </summary>
public class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, "Regra de negócio violada"),
            BadHttpRequestException bad => (bad.StatusCode, "Requisição inválida"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Não autenticado"),
            NotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflito com dados existentes"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno")
        };

        // Erro inesperado: registra o detalhe no log, mas não o expõe para quem chamou.
        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Erro não tratado em {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = status,
                Title = title,
                Detail = exception switch
                {
                    _ when status == StatusCodes.Status500InternalServerError =>
                        "Ocorreu um erro inesperado. Tente novamente mais tarde.",
                    // A mensagem original do ASP.NET é técnica e em inglês.
                    BadHttpRequestException =>
                        "O corpo da requisição não é um JSON válido ou tem um valor de tipo errado " +
                        "(ex.: texto no lugar de número ou um status que não existe).",
                    _ => exception.Message
                }
            }
        });
    }
}
