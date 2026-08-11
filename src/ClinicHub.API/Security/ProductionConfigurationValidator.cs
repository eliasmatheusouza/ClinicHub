namespace ClinicHub.API.Security;

public static class ProductionConfigurationValidator
{
    public static void Validate(IConfiguration configuration)
    {
        var errors = new List<string>();
        var jwtKey = configuration["Jwt:Key"];
        var allowedHosts = configuration["AllowedHosts"];
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var frontendUrl = configuration["EmailConfirmation:FrontendUrl"];
        var emailDeliveryMode = configuration["Email:DeliveryMode"];

        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32 || jwtKey.Contains("development", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Jwt:Key deve ser uma chave não previsível, exclusiva de produção e ter ao menos 32 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts == "*")
        {
            errors.Add("AllowedHosts deve restringir os hosts públicos da API em produção.");
        }

        if (origins.Length == 0 || origins.Any(origin => !IsHttpsUri(origin)))
        {
            errors.Add("Cors:AllowedOrigins deve conter apenas origens HTTPS válidas em produção.");
        }

        if (!IsHttpsUri(frontendUrl))
        {
            errors.Add("EmailConfirmation:FrontendUrl deve usar HTTPS em produção.");
        }

        if (!string.Equals(emailDeliveryMode, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Email:DeliveryMode deve ser Smtp em produção; links de confirmação não podem ser enviados a logs.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Configuração de Production inválida: {string.Join(" ", errors)}");
        }
    }

    private static bool IsHttpsUri(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
