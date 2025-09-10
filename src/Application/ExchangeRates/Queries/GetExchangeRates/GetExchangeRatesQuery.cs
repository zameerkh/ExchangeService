using ExchangeService.Domain.Entities;
using MediatR;

namespace ExchangeService.Application.ExchangeRates.Queries.GetExchangeRates;

/// <summary>
/// Query to get exchange rates for specified currencies
/// </summary>
public class GetExchangeRatesQuery : IRequest<GetExchangeRatesResult>
{
    public string InputCurrency { get; init; } = string.Empty;
    public IEnumerable<string> OutputCurrencies { get; init; } = Enumerable.Empty<string>();
}

/// <summary>
/// Result containing exchange rates
/// </summary>
public class GetExchangeRatesResult
{
    public string BaseCurrency { get; init; } = string.Empty;
    public IEnumerable<ExchangeRateDto> ExchangeRates { get; init; } = Enumerable.Empty<ExchangeRateDto>();
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// DTO for exchange rate information
/// </summary>
public class ExchangeRateDto
{
    public string Currency { get; init; } = string.Empty;
    public decimal Rate { get; init; }
    public DateTime Timestamp { get; init; }
}
