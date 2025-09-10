namespace ExchangeService.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount with its currency
/// </summary>
public class Money : IEquatable<Money>
{
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; } = null!;

    private Money() { } // For EF Core

    public Money(decimal amount, Currency currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        Amount = amount;
        Currency = currency ?? throw new ArgumentNullException(nameof(currency));
    }

    public static Money Create(decimal amount, Currency currency) => new(amount, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency.Code} and {other.Currency.Code}");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract {other.Currency.Code} from {Currency.Code}");

        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        return new Money(Amount * factor, Currency);
    }

    public Money Divide(decimal divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException("Cannot divide by zero");

        return new Money(Amount / divisor, Currency);
    }

    public bool Equals(Money? other)
    {
        return other is not null && Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj)
    {
        return obj is Money money && Equals(money);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Amount, Currency);
    }

    public static bool operator ==(Money? left, Money? right)
    {
        return EqualityComparer<Money>.Default.Equals(left, right);
    }

    public static bool operator !=(Money? left, Money? right)
    {
        return !(left == right);
    }

    public override string ToString() => $"{Amount:F2} {Currency.Code}";
}
