using ClinicHub.Domain.Authentication;
using ClinicHub.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClinicHub.Infrastructure.Persistence.Repositories;

internal sealed class RefreshTokenRepository(ClinicHubDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        context.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default) =>
        context.RefreshTokens.AddAsync(refreshToken, cancellationToken).AsTask();

    public void Update(RefreshToken refreshToken) => context.RefreshTokens.Update(refreshToken);
}
