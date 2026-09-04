using FluentValidation;
using FyM.Users.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FyM.Users.Api.Middleware;

/// <summary>
/// Único punto donde una excepción se traduce a una respuesta HTTP.
/// Los controladores nunca usan <c>try/catch</c>: lanzan excepciones de
/// dominio y este handler las mapea a <c>ProblemDetails</c> (RFC 7807),
/// sin exponer mensajes internos ni stack traces fuera de Development.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, errors) = Map(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Error no controlado procesando {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Error de negocio ({StatusCode}) procesando {Method} {Path}: {Message}",
                statusCode, httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        var showDetail = statusCode < StatusCodes.Status500InternalServerError || environment.IsDevelopment();
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = showDetail ? exception.Message : "Ocurrió un error inesperado. Intente nuevamente más tarde.",
            Instance = httpContext.Request.Path,
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        httpContext.Response.ContentType = "application/problem+json";
        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Title, IDictionary<string, string[]>? Errors) Map(Exception exception) => exception switch
    {
        ValidationException validationException => (
            StatusCodes.Status400BadRequest,
            "Uno o más campos no son válidos.",
            validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),
        NotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado.", null),
        ConflictException => (StatusCodes.Status409Conflict, "Conflicto con el estado actual de los datos.", null),
        ForbiddenOperationException => (StatusCodes.Status403Forbidden, "Operación no permitida.", null),
        BusinessRuleException => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio violada.", null),
        DbUpdateConcurrencyException => (
            StatusCodes.Status409Conflict, "El recurso fue modificado por otro usuario. Recargue e intente de nuevo.", null),
        _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor.", null),
    };
}
