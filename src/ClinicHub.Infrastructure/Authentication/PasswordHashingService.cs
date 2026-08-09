using ClinicHub.Application.Authentication.Abstractions;
using Microsoft.AspNetCore.Identity;

namespace ClinicHub.Infrastructure.Authentication;

internal sealed class PasswordHashingService : IPasswordHashingService
{
    private readonly PasswordHasher<object> _passwordHasher = new();

    public string Hash(string password) => _passwordHasher.HashPassword(new object(), password);

    public bool Verify(string passwordHash, string password) =>
        _passwordHasher.VerifyHashedPassword(new object(), passwordHash, password) is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
