using System.Net;
using System.Net.Mail;
using ClinicHub.Application.Authentication.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClinicHub.Infrastructure.Authentication;

internal sealed class EmailConfirmationSender(
    IConfiguration configuration,
    ILogger<EmailConfirmationSender> logger) : IEmailConfirmationSender
{
    public async Task SendAsync(string recipientEmail, string confirmationToken, CancellationToken cancellationToken = default)
    {
        var frontendUrl = configuration["EmailConfirmation:FrontendUrl"] ?? "http://localhost:4200";
        var confirmationUrl = $"{frontendUrl.TrimEnd('/')}/confirm-email?token={Uri.EscapeDataString(confirmationToken)}";
        var deliveryMode = configuration["Email:DeliveryMode"] ?? "Log";

        if (string.Equals(deliveryMode, "Log", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Email confirmation link generated for {RecipientEmail}: {ConfirmationUrl}", recipientEmail, confirmationUrl);
            return;
        }

        var host = configuration["Email:Smtp:Host"]
            ?? throw new InvalidOperationException("Email:Smtp:Host deve ser configurado para envio SMTP.");
        var from = configuration["Email:From"]
            ?? throw new InvalidOperationException("Email:From deve ser configurado para envio SMTP.");
        var port = int.TryParse(configuration["Email:Smtp:Port"], out var configuredPort) ? configuredPort : 587;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = bool.TryParse(configuration["Email:Smtp:UseSsl"], out var useSsl) && useSsl
        };
        var username = configuration["Email:Smtp:Username"];
        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, configuration["Email:Smtp:Password"]);
        }

        using var message = new MailMessage(from, recipientEmail)
        {
            Subject = "Confirme sua conta ClinicHub",
            Body = $"Confirme sua conta em até 24 horas: {confirmationUrl}",
            IsBodyHtml = false
        };
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message);
        logger.LogInformation("Email confirmation sent to {RecipientEmail}", recipientEmail);
    }
}
