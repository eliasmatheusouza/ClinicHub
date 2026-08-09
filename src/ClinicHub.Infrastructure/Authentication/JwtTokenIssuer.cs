using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClinicHub.Application.Authentication.Abstractions;
using ClinicHub.Application.Authentication.Dtos;
using ClinicHub.Domain.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClinicHub.Infrastructure.Authentication;

internal sealed class JwtTokenIssuer(IConfiguration configuration) : ITokenIssuer
{
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer deve ser configurado.");
    private readonly string _audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience deve ser configurado.");
    private readonly string _key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key deve ser configurado.");
    private readonly int _accessTokenLifetimeMinutes = ParsePositiveInteger(configuration["Jwt:AccessTokenLifetimeMinutes"], 15);
    private readonly int _refreshTokenLifetimeDays = ParsePositiveInteger(configuration["Jwt:RefreshTokenLifetimeDays"], 7);

    public AccessTokenIssue CreateAccessToken(User user)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_accessTokenLifetimeMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_issuer, _audience, claims, expires: expiresAtUtc, signingCredentials: signingCredentials);

        return new(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public string CreateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string refreshToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    public DateTime GetRefreshTokenExpiryUtc(DateTime issuedAtUtc) => issuedAtUtc.AddDays(_refreshTokenLifetimeDays);

    private static int ParsePositiveInteger(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) && parsed > 0 ? parsed : defaultValue;
}
