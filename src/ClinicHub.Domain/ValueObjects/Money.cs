using ClinicHub.Domain.Common;

namespace ClinicHub.Domain.ValueObjects;

public sealed record Money
{
    private Money()
    {
        Currency = null!;
    }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    public static DomainResult<Money> Create(decimal amount, string? currency = "BRL")
    {
        var normalizedCurrency = currency?.Trim().ToUpperInvariant();

        if (amount <= 0 || decimal.Round(amount, 2) != amount)
        {
            return DomainResult<Money>.Failure(new("money.amount.invalid", "O valor deve ser positivo e ter no máximo duas casas decimais."));
        }

        if (string.IsNullOrWhiteSpace(normalizedCurrency) || normalizedCurrency.Length != 3 || !normalizedCurrency.All(char.IsLetter))
        {
            return DomainResult<Money>.Failure(new("money.currency.invalid", "A moeda deve usar um código ISO de três letras."));
        }

        return DomainResult<Money>.Success(new Money(amount, normalizedCurrency));
    }
}
