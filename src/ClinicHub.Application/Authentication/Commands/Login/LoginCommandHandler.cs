using ClinicHub.Application.Abstractions;
using ClinicHub.Application.Authentication.Abstractions;
using ClinicHub.Application.Authentication.Dtos;
using ClinicHub.Application.Common;
using ClinicHub.Domain.Authentication;
using ClinicHub.Domain.Interfaces;
using ClinicHub.Domain.ValueObjects;
using MediatR;

namespace ClinicHub.Application.Authentication.Commands.Login;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IUnitOfWork unitOfWork,
    IPasswordHashingService passwordHashingService,
    ITokenIssuer tokenIssuer,
    IClock clock) : IRequestHandler<LoginCommand, ApplicationResult<AuthenticationTokensDto>>
{
    public async Task<ApplicationResult<AuthenticationTokensDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var emailResult = EmailAddress.Create(request.Email);
        if (!emailResult.IsSuccess)
        {
            return ApplicationResult<AuthenticationTokensDto>.FailureFromDomain(emailResult.Notifications);
        }

        var user = await userRepository.GetByEmailAsync(emailResult.Value!, cancellationToken);
        if (user is null || !user.IsActive || !passwordHashingService.Verify(user.PasswordHash, request.Password))
        {
            return ApplicationResult<AuthenticationTokensDto>.Failure(new("auth.invalid_credentials", "E-mail ou senha inválidos."));
        }

        var issuedAtUtc = clock.UtcNow;
        var refreshTokenValue = tokenIssuer.CreateRefreshToken();
        var refreshTokenResult = RefreshToken.Create(
            Guid.NewGuid(),
            user.Id,
            tokenIssuer.HashRefreshToken(refreshTokenValue),
            tokenIssuer.GetRefreshTokenExpiryUtc(issuedAtUtc),
            issuedAtUtc);

        if (!refreshTokenResult.IsSuccess)
        {
            return ApplicationResult<AuthenticationTokensDto>.FailureFromDomain(refreshTokenResult.Notifications);
        }

        await refreshTokenRepository.AddAsync(refreshTokenResult.Value!, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = tokenIssuer.CreateAccessToken(user);
        return ApplicationResult<AuthenticationTokensDto>.Success(new(accessToken.Value, accessToken.ExpiresAtUtc, refreshTokenValue));
    }
}
