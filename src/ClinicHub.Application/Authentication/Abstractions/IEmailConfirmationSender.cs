namespace ClinicHub.Application.Authentication.Abstractions;

public interface IEmailConfirmationSender
{
    Task SendAsync(string recipientEmail, string confirmationToken, CancellationToken cancellationToken = default);
}
