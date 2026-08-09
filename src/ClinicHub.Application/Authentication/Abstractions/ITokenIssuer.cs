using ClinicHub.Application.Authentication.Dtos;
using ClinicHub.Domain.Authentication;

namespace ClinicHub.Application.Authentication.Abstractions;

public interface ITokenIssuer
{
    AccessTokenIssue CreateAccessToken(User user);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
    DateTime GetRefreshTokenExpiryUtc(DateTime issuedAtUtc);
}
