using ClinicHub.Domain.Authentication;
using ClinicHub.Domain.ValueObjects;

namespace ClinicHub.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(EmailAddress email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailConfirmationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<User>> GetActiveDoctorsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
