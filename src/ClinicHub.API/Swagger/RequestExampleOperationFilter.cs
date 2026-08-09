using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ClinicHub.API.Swagger;

internal sealed class RequestExampleOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody is null)
        {
            return;
        }

        var example = (context.ApiDescription.RelativePath?.ToLowerInvariant(), context.ApiDescription.HttpMethod?.ToUpperInvariant()) switch
        {
            ("api/auth/login", "POST") => Object(("email", "admin@clinichub.local"), ("password", "Admin123!")),
            ("api/auth/register", "POST") => Object(("email", "paciente@exemplo.com"), ("password", "SenhaSegura1"), ("confirmPassword", "SenhaSegura1")),
            ("api/auth/confirm-email", "POST") => Object(("token", "token-recebido-no-link-de-confirmacao")),
            ("api/auth/refresh", "POST") => Object(("refreshToken", "refresh-token-retornado-no-login")),
            ("api/patients", "POST") => Object(("name", "Maria da Silva"), ("birthDate", "1990-05-20"), ("email", "maria.silva@exemplo.com"), ("phone", "+5511999999999")),
            (var path, "PUT") when path?.StartsWith("api/patients/") is true => Object(("name", "Maria da Silva"), ("birthDate", "1990-05-20"), ("email", "maria.silva@exemplo.com"), ("phone", "+5511988888888")),
            ("api/appointments", "POST") => Object(("patientId", "11111111-1111-1111-1111-111111111111"), ("doctorId", "22222222-2222-2222-2222-222222222222"), ("startUtc", "2027-01-15T14:00:00Z"), ("durationMinutes", 30)),
            (var path, "PUT") when path?.EndsWith("/schedule") is true => Object(("startUtc", "2027-01-16T15:00:00Z"), ("durationMinutes", 45)),
            (var path, "POST") when path?.EndsWith("/cancel") is true => Object(("reason", "Solicitação do paciente.")),
            ("api/payments", "POST") => Object(("appointmentId", "33333333-3333-3333-3333-333333333333"), ("amount", 150.75m), ("currency", "BRL"), ("method", 4)),
            _ => null
        };

        if (example is null)
        {
            return;
        }

        foreach (var mediaType in operation.RequestBody.Content.Values)
        {
            mediaType.Example = example;
        }
    }

    private static OpenApiObject Object(params (string Key, object Value)[] properties)
    {
        var result = new OpenApiObject();
        foreach (var (key, value) in properties)
        {
            result[key] = value switch
            {
                int integer => new OpenApiInteger(integer),
                decimal decimalValue => new OpenApiDouble((double)decimalValue),
                _ => new OpenApiString(value.ToString())
            };
        }

        return result;
    }
}
