using ExchangeService.Domain.Entities;
using ExchangeService.Domain.ValueObjects;

namespace ExchangeService.Application.Common.Interfaces;

/// <summary>
/// Interface for external exchange rate data providers
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Gets the exchange rate between two currencies
    /// </summary>
    Task<ExchangeRate?> GetExchangeRateAsync(Currency baseCurrency, Currency targetCurrency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple exchange rates for a base currency
    /// </summary>
    Task<IEnumerable<ExchangeRate>> GetExchangeRatesAsync(Currency baseCurrency, IEnumerable<Currency> targetCurrencies, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available exchange rates for a base currency
    /// </summary>
    Task<IEnumerable<ExchangeRate>> GetAllExchangeRatesAsync(Currency baseCurrency, CancellationToken cancellationToken = default);
}
