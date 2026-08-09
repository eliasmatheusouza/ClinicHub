using ClinicHub.Domain.Common;

namespace ClinicHub.Domain.ValueObjects;

public sealed record PersonName
{
    private PersonName()
    {
        Value = null!;
    }

    private PersonName(string value)
    {
        Value = value;
    }

    public string Value { get; private set; }

    public static DomainResult<PersonName> Create(string? value)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return DomainResult<PersonName>.Failure(new("person_name.required", "O nome é obrigatório."));
        }

        if (normalized.Length is < 2 or > 120)
        {
            return DomainResult<PersonName>.Failure(new("person_name.length", "O nome deve ter entre 2 e 120 caracteres."));
        }

        if (normalized.Any(char.IsDigit))
        {
            return DomainResult<PersonName>.Failure(new("person_name.invalid", "O nome não pode conter números."));
        }

        return DomainResult<PersonName>.Success(new PersonName(normalized));
    }
}
