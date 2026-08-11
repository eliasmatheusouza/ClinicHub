using ClinicHub.Domain.Appointments;
using ClinicHub.Domain.Authentication;
using ClinicHub.Domain.Patients;
using ClinicHub.Domain.Payments;
using ClinicHub.Infrastructure.Persistence.Auditing;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Infrastructure.Persistence;

public sealed class ClinicHubDbContext(DbContextOptions<ClinicHubDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicHubDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
