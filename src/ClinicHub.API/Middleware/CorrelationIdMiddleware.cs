using Serilog.Context;

namespace ClinicHub.API.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var requestedCorrelationId = context.Request.Headers[HeaderName].ToString();
        var correlationId = IsValid(requestedCorrelationId) ? requestedCorrelationId : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static bool IsValid(string correlationId) =>
        !string.IsNullOrWhiteSpace(correlationId)
        && correlationId.Length <= 128
        && !correlationId.Contains('\r')
        && !correlationId.Contains('\n');
}
