using ClinicHub.Application.Auditing;
using ClinicHub.Infrastructure.Persistence;
using ClinicHub.Infrastructure.Persistence.Auditing;

namespace ClinicHub.Infrastructure.Auditing;

internal sealed class EfAuditTrailWriter(ClinicHubDbContext context) : IAuditTrailWriter
{
    public async Task WriteAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        var auditLog = AuditLog.Create(
            record.ActorUserId,
            record.ActorRole,
            record.Action,
            record.ResourcePath,
            record.StatusCode,
            record.CorrelationId,
            record.OccurredAtUtc);

        await context.Set<AuditLog>().AddAsync(auditLog, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
