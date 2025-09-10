using ExchangeService.Domain.ValueObjects;

namespace ExchangeService.Domain.Entities;

/// <summary>
/// Represents an exchange rate between two currencies at a specific point in time
/// </summary>
public class ExchangeRate
{
    public Currency BaseCurrency { get; private set; } = null!;
    public Currency TargetCurrency { get; private set; } = null!;
    public decimal Rate { get; private set; }
    public DateTime Timestamp { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    private ExchangeRate() { } // For EF Core

    public ExchangeRate(Currency baseCurrency, Currency targetCurrency, decimal rate, DateTime timestamp, DateTime? expiresAt = null)
    {
        if (rate <= 0)
            throw new ArgumentException("Exchange rate must be positive", nameof(rate));

        BaseCurrency = baseCurrency ?? throw new ArgumentNullException(nameof(baseCurrency));
        TargetCurrency = targetCurrency ?? throw new ArgumentNullException(nameof(targetCurrency));
        Rate = rate;
        Timestamp = timestamp;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;

    public Money Convert(Money amount)
    {
        if (amount.Currency != BaseCurrency)
            throw new InvalidOperationException($"Cannot convert {amount.Currency.Code} using exchange rate for {BaseCurrency.Code}");

        var convertedAmount = amount.Amount * Rate;
        return new Money(convertedAmount, TargetCurrency);
    }
}
