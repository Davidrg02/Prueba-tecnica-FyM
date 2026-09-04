using Serilog.Context;

namespace FyM.Users.Api.Middleware;

/// <summary>Propaga (o genera) un <c>X-Correlation-Id</c> por request y lo
/// añade al contexto de log de Serilog, para poder rastrear todas las
/// líneas de log de una misma petición end-to-end.</summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing) && !string.IsNullOrWhiteSpace(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
