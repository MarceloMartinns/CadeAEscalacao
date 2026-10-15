using CadeAEscalacao.Application.Common;
using CadeAEscalacao.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CadeAEscalacao.Server.ErrorHandling;

/// <summary>
/// Turns the known exceptions into Problem Details (RFC 9457), as decided in ADR-011, ADR-012 and ADR-013.
/// Any other exception is left to the default handler (500, without details outside development).
/// </summary>
internal sealed class ProblemDetailsExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int Status, string Title, string Detail)? problem = exception switch
        {
            // Body that is not valid JSON (e.g. an unknown enum value). Outside development the
            // framework answers 400 by itself; in development it throws so the error is visible.
            BadHttpRequestException badRequest => (badRequest.StatusCode, "Bad request", exception.Message),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Concurrent update",
                "Someone else changed this data at the same time. Reload and try again."),
            // Unique index hit by two requests at the same time (e.g. two lists for the same date).
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } => (
                StatusCodes.Status409Conflict,
                "Conflict",
                "This data already exists. Reload and try again."),
            DomainException => (StatusCodes.Status422UnprocessableEntity, "Business rule violated", exception.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid argument", exception.Message),
            _ => null,
        };

        if (problem is not { } known)
        {
            return false;
        }

        httpContext.Response.StatusCode = known.Status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = known.Status, Title = known.Title, Detail = known.Detail },
        });
    }
}
