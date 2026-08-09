using ClinicHub.Domain.Common;

namespace ClinicHub.Domain.ValueObjects;

public sealed record PhoneNumber
{
    private PhoneNumber()
    {
        Value = null!;
    }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }

    public static DomainResult<PhoneNumber> Create(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length is < 10 or > 15)
        {
            return DomainResult<PhoneNumber>.Failure(new("phone.invalid", "O telefone deve ter entre 10 e 15 dígitos."));
        }

        return DomainResult<PhoneNumber>.Success(new PhoneNumber(digits));
    }
}
