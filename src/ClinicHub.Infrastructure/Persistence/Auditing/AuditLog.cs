namespace ClinicHub.Infrastructure.Persistence.Auditing;

public sealed class AuditLog
{
    private AuditLog()
    {
    }

    private AuditLog(
        Guid? actorUserId,
        string? actorRole,
        string action,
        string resourcePath,
        int statusCode,
        string correlationId,
        DateTime occurredAtUtc)
    {
        Id = Guid.NewGuid();
        ActorUserId = actorUserId;
        ActorRole = actorRole;
        Action = action;
        ResourcePath = resourcePath;
        StatusCode = statusCode;
        CorrelationId = correlationId;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? ActorRole { get; private set; }
    public string Action { get; private set; } = null!;
    public string ResourcePath { get; private set; } = null!;
    public int StatusCode { get; private set; }
    public string CorrelationId { get; private set; } = null!;
    public DateTime OccurredAtUtc { get; private set; }

    public static AuditLog Create(
        Guid? actorUserId,
        string? actorRole,
        string action,
        string resourcePath,
        int statusCode,
        string correlationId,
        DateTime occurredAtUtc) =>
        new(actorUserId, actorRole, action, resourcePath, statusCode, correlationId, occurredAtUtc);
}
