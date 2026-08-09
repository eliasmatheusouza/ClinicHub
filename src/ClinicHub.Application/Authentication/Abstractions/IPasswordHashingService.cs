namespace ClinicHub.Application.Authentication.Abstractions;

public interface IPasswordHashingService
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}
