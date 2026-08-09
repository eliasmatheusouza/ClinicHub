using System.Security.Cryptography;
using ClinicHub.Application.Authentication.Abstractions;

namespace ClinicHub.Infrastructure.Authentication;

internal sealed class EmailConfirmationTokenService : IEmailConfirmationTokenService
{
    public string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    public string HashToken(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
