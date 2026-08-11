namespace ClinicHub.Application.Auditing;

public sealed record AuditRecord(
    Guid? ActorUserId,
    string? ActorRole,
    string Action,
    string ResourcePath,
    int StatusCode,
    string CorrelationId,
    DateTime OccurredAtUtc);
