using ClinicHub.Domain.Authentication;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.ValueObjects;
using ClinicHub.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(ClinicHubDbContext context) : IUserRepository
{
    public Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default) =>
        context.Users.SingleOrDefaultAsync(user => user.Email.Value == email.Value, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByEmailConfirmationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        context.Users.SingleOrDefaultAsync(user => user.EmailConfirmationTokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyCollection<User>> GetActiveDoctorsAsync(CancellationToken cancellationToken = default) =>
        await context.Users.AsNoTracking()
            .Where(user => user.IsActive && user.Role == UserRole.Doctor)
            .OrderBy(user => user.Email.Value)
            .ToListAsync(cancellationToken);

    public Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        context.Users.AddAsync(user, cancellationToken).AsTask();
}
