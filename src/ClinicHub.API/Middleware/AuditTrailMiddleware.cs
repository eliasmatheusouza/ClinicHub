using System.Security.Claims;
using ClinicHub.Application.Auditing;
using Serilog;

namespace ClinicHub.API.Middleware;

public sealed class AuditTrailMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAuditTrailWriter auditTrailWriter)
    {
        if (!ShouldAudit(context.Request))
        {
            await next(context);
            return;
        }

        try
        {
            await next(context);
        }
        catch
        {
            await WriteAuditSafelyAsync(context, auditTrailWriter, StatusCodes.Status500InternalServerError);
            throw;
        }

        await WriteAuditSafelyAsync(context, auditTrailWriter, context.Response.StatusCode);
    }

    private static bool ShouldAudit(HttpRequest request) =>
        (HttpMethods.IsPost(request.Method)
            || HttpMethods.IsPut(request.Method)
            || HttpMethods.IsPatch(request.Method)
            || HttpMethods.IsDelete(request.Method))
        && request.Path.StartsWithSegments("/api")
        && !request.Path.StartsWithSegments("/api/auth");

    private static async Task WriteAuditSafelyAsync(HttpContext context, IAuditTrailWriter auditTrailWriter, int statusCode)
    {
        try
        {
            var actorUserId = Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
                ? (Guid?)userId
                : null;
            var record = new AuditRecord(
                actorUserId,
                context.User.FindFirstValue(ClaimTypes.Role),
                context.Request.Method,
                context.Request.Path.Value ?? "/",
                statusCode,
                context.TraceIdentifier,
                DateTime.UtcNow);

            await auditTrailWriter.WriteAsync(record, CancellationToken.None);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Could not persist audit record for {Method} {Path} with correlation {CorrelationId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.TraceIdentifier);
        }
    }
}
