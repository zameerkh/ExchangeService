using FluentValidation;

namespace ExchangeService.Api.Models;

/// <summary>
/// FluentValidation validator for ExchangeRequest
/// </summary>
public class ExchangeRequestValidator : AbstractValidator<ExchangeRequest>
{
    // Extended list of commonly supported currency codes for validation
    // In production, consider loading this from configuration or an external service
    private static readonly HashSet<string> ValidCurrencyCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Major currencies
        "USD", "EUR", "GBP", "JPY", "AUD", "CAD", "CHF", "CNY", "SEK", "NZD",
        "MXN", "SGD", "HKD", "NOK", "TRY", "RUB", "INR", "BRL", "ZAR", "KRW",
        // Additional supported currencies
        "AED", "AFN", "ALL", "AMD", "ANG", "AOA", "ARS", "AWG", "AZN", "BAM",
        "BBD", "BDT", "BGN", "BHD", "BIF", "BMD", "BND", "BOB", "BSD", "BTN",
        "BWP", "BYN", "BZD", "CDF", "CLP", "COP", "CRC", "CUP", "CVE", "CZK",
        "DJF", "DKK", "DOP", "DZD", "EGP", "ERN", "ETB", "FJD", "FKP", "GEL",
        "GGP", "GHS", "GIP", "GMD", "GNF", "GTQ", "GYD", "HNL", "HRK", "HTG",
        "HUF", "IDR", "ILS", "IMP", "IQD", "IRR", "ISK", "JEP", "JMD", "JOD",
        "KES", "KGS", "KHR", "KMF", "KWD", "KYD", "KZT", "LAK", "LBP", "LKR",
        "LRD", "LSL", "LYD", "MAD", "MDL", "MGA", "MKD", "MMK", "MNT", "MOP",
        "MRU", "MUR", "MVR", "MWK", "MYR", "MZN", "NAD", "NGN", "NIO", "NPR",
        "OMR", "PAB", "PEN", "PGK", "PHP", "PKR", "PLN", "PYG", "QAR", "RON",
        "RSD", "RWF", "SAR", "SBD", "SCR", "SDG", "SHP", "SLE", "SLL", "SOS",
        "SRD", "SSP", "STN", "SYP", "SZL", "THB", "TJS", "TMT", "TND", "TOP",
        "TTD", "TWD", "TZS", "UAH", "UGX", "UYU", "UZS", "VES", "VND", "VUV",
        "WST", "XAF", "XCD", "XDR", "XOF", "XPF", "YER", "ZMW", "ZWL"
        // Production note: Consider loading from appsettings.json or external API
    };

    public ExchangeRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(1_000_000)
            .WithMessage("Amount cannot exceed 1,000,000 for this service");

        RuleFor(x => x.InputCurrency)
            .NotEmpty()
            .WithMessage("Input currency is required")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters")
            .Must(BeValidCurrencyCode)
            .WithMessage("Invalid input currency code");

        RuleFor(x => x.OutputCurrency)
            .NotEmpty()
            .WithMessage("Output currency is required")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters")
            .Must(BeValidCurrencyCode)
            .WithMessage("Invalid output currency code");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.InputCurrency, x.OutputCurrency, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Input and output currencies must be different");
    }

    private static bool BeValidCurrencyCode(string currencyCode)
    {
        return !string.IsNullOrWhiteSpace(currencyCode) && 
               ValidCurrencyCodes.Contains(currencyCode);
    }
}