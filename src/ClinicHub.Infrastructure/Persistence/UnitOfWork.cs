using ClinicHub.Domain.Interfaces;

namespace ClinicHub.Infrastructure.Persistence;

internal sealed class UnitOfWork(ClinicHubDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
