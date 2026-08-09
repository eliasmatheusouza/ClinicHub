using System.Text.RegularExpressions;
using ClinicHub.Domain.Common;

namespace ClinicHub.Domain.ValueObjects;

public sealed partial record EmailAddress
{
    private EmailAddress()
    {
        Value = null!;
    }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }

    public static DomainResult<EmailAddress> Create(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized) || !EmailPattern().IsMatch(normalized))
        {
            return DomainResult<EmailAddress>.Failure(new("email.invalid", "O e-mail informado é inválido."));
        }

        return DomainResult<EmailAddress>.Success(new EmailAddress(normalized));
    }

    [GeneratedRegex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}
