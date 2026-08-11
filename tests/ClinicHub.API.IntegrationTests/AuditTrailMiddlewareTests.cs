using System.Security.Claims;
using ClinicHub.API.Middleware;
using ClinicHub.Application.Auditing;
using Microsoft.AspNetCore.Http;

namespace ClinicHub.API.IntegrationTests;

public sealed class AuditTrailMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenApiMutationSucceeds_PersistsSafeAuditRecord()
    {
        // Arrange
        var actorId = Guid.NewGuid();
        var writer = new RecordingAuditTrailWriter();
        var middleware = new AuditTrailMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Delete;
        context.Request.Path = "/api/patients/3ed1e9c1-79aa-4f4c-bda2-0e95df90105c";
        context.TraceIdentifier = "audit-test-correlation";
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        ], "test"));

        // Act
        await middleware.InvokeAsync(context, writer);

        // Assert
        var record = Assert.Single(writer.Records);
        Assert.Equal(actorId, record.ActorUserId);
        Assert.Equal("Admin", record.ActorRole);
        Assert.Equal("DELETE", record.Action);
        Assert.Equal(context.Request.Path.Value, record.ResourcePath);
        Assert.Equal(StatusCodes.Status204NoContent, record.StatusCode);
        Assert.Equal("audit-test-correlation", record.CorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestIsAuthenticationOrReadOnly_DoesNotPersistAuditRecord()
    {
        // Arrange
        var writer = new RecordingAuditTrailWriter();
        var middleware = new AuditTrailMiddleware(_ => Task.CompletedTask);
        var authenticationContext = new DefaultHttpContext();
        authenticationContext.Request.Method = HttpMethods.Post;
        authenticationContext.Request.Path = "/api/auth/login";
        var readOnlyContext = new DefaultHttpContext();
        readOnlyContext.Request.Method = HttpMethods.Get;
        readOnlyContext.Request.Path = "/api/patients";

        // Act
        await middleware.InvokeAsync(authenticationContext, writer);
        await middleware.InvokeAsync(readOnlyContext, writer);

        // Assert
        Assert.Empty(writer.Records);
    }

    private sealed class RecordingAuditTrailWriter : IAuditTrailWriter
    {
        public List<AuditRecord> Records { get; } = [];

        public Task WriteAsync(AuditRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }
    }
}
