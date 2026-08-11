using ClinicHub.Infrastructure.Persistence.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicHub.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).ValueGeneratedNever();
        builder.Property(log => log.ActorRole).HasMaxLength(20);
        builder.Property(log => log.Action).HasMaxLength(10).IsRequired();
        builder.Property(log => log.ResourcePath).HasMaxLength(256).IsRequired();
        builder.Property(log => log.CorrelationId).HasMaxLength(128).IsRequired();
        builder.Property(log => log.StatusCode).IsRequired();
        builder.Property(log => log.OccurredAtUtc).IsRequired();
        builder.HasIndex(log => log.OccurredAtUtc);
        builder.HasIndex(log => new { log.ActorUserId, log.OccurredAtUtc });
        builder.HasIndex(log => log.CorrelationId);
    }
}
