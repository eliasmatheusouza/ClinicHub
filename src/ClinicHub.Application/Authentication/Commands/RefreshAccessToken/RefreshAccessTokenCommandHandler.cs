using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Authentication.Abstractions;
using ClinicHub.Application.Authentication.Dtos;
using ClinicHub.Application.Common;
using ClinicHub.Domain.Authentication;
using ClinicHub.Domain.Interfaces;
using MediatR;

namespace ClinicHub.Application.Authentication.Commands.RefreshAccessToken;

public sealed class RefreshAccessTokenCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ITokenIssuer tokenIssuer,
    IClock clock) : IRequestHandler<RefreshAccessTokenCommand, ApplicationResult<AuthenticationTokensDto>>
{
    public async Task<ApplicationResult<AuthenticationTokensDto>> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var refreshToken = await refreshTokenRepository.GetByTokenHashAsync(tokenIssuer.HashRefreshToken(request.RefreshToken), cancellationToken);
        if (refreshToken is null || !refreshToken.IsActive(now))
        {
            return ApplicationResult<AuthenticationTokensDto>.Failure(new("auth.invalid_refresh_token", "Refresh token inválido ou expirado."));
        }

        var user = await userRepository.GetByIdAsync(refreshToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return ApplicationResult<AuthenticationTokensDto>.Failure(new("auth.invalid_refresh_token", "Refresh token inválido ou expirado."));
        }

        var revokeResult = refreshToken.Revoke(now);
        if (!revokeResult.IsSuccess)
        {
            return ApplicationResult<AuthenticationTokensDto>.FailureFromDomain(revokeResult.Notifications);
        }

        var nextRefreshTokenValue = tokenIssuer.CreateRefreshToken();
        var nextRefreshTokenResult = RefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            tokenIssuer.HashRefreshToken(nextRefreshTokenValue),
            tokenIssuer.GetRefreshTokenExpiryUtc(now),
            now);

        if (!nextRefreshTokenResult.IsSuccess)
        {
            return ApplicationResult<AuthenticationTokensDto>.FailureFromDomain(nextRefreshTokenResult.Notifications);
        }

        refreshTokenRepository.Update(refreshToken);
        await refreshTokenRepository.AddAsync(nextRefreshTokenResult.Value!, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = tokenIssuer.CreateAccessToken(user);
        return ApplicationResult<AuthenticationTokensDto>.Success(new(accessToken.Value, accessToken.ExpiresAtUtc, nextRefreshTokenValue));
    }
}
