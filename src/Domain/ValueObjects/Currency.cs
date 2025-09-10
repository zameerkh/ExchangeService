namespace ExchangeService.Domain.ValueObjects;

/// <summary>
/// Represents a currency as a value object
/// </summary>
public class Currency : IEquatable<Currency>
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private Currency() { } // For EF Core

    public Currency(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Currency code cannot be null or empty", nameof(code));

        if (code.Length != 3)
            throw new ArgumentException("Currency code must be exactly 3 characters", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Currency name cannot be null or empty", nameof(name));

        Code = code.ToUpperInvariant();
        Name = name;
    }

    public static Currency Create(string code, string name) => new(code, name);

    // Common currencies
    public static Currency USD => new("USD", "US Dollar");
    public static Currency EUR => new("EUR", "Euro");
    public static Currency GBP => new("GBP", "British Pound");
    public static Currency JPY => new("JPY", "Japanese Yen");
    public static Currency CAD => new("CAD", "Canadian Dollar");
    public static Currency AUD => new("AUD", "Australian Dollar");
    public static Currency CHF => new("CHF", "Swiss Franc");
    public static Currency CNY => new("CNY", "Chinese Yuan");

    public bool Equals(Currency? other)
    {
        return other is not null && Code == other.Code;
    }

    public override bool Equals(object? obj)
    {
        return obj is Currency currency && Equals(currency);
    }

    public override int GetHashCode()
    {
        return Code.GetHashCode();
    }

    public static bool operator ==(Currency? left, Currency? right)
    {
        return EqualityComparer<Currency>.Default.Equals(left, right);
    }

    public static bool operator !=(Currency? left, Currency? right)
    {
        return !(left == right);
    }

    public override string ToString() => Code;
}
