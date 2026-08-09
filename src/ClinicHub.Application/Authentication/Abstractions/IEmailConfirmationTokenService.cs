namespace ClinicHub.Application.Authentication.Abstractions;

public interface IEmailConfirmationTokenService
{
    string CreateToken();
    string HashToken(string token);
}
