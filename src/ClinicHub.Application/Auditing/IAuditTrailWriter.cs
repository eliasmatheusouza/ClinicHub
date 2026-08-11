namespace ClinicHub.Application.Auditing;

public interface IAuditTrailWriter
{
    Task WriteAsync(AuditRecord record, CancellationToken cancellationToken = default);
}
