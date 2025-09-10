using ExchangeService.Domain.ValueObjects;
using MediatR;

namespace ExchangeService.Application.ExchangeRates.Commands.ConvertCurrency;

/// <summary>
/// Command to convert an amount from one currency to another
/// </summary>
public class ConvertCurrencyCommand : IRequest<ConvertCurrencyResult>
{
    public decimal Amount { get; init; }
    public string InputCurrency { get; init; } = string.Empty;
    public string OutputCurrency { get; init; } = string.Empty;
}

/// <summary>
/// Result of currency conversion
/// </summary>
public class ConvertCurrencyResult
{
    public Money OriginalAmount { get; init; } = null!;
    public Money ConvertedAmount { get; init; } = null!;
    public decimal ExchangeRate { get; init; }
    public DateTime Timestamp { get; init; }
}
