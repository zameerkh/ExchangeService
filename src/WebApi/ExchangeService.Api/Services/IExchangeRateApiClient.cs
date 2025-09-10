using ExchangeService.Api.Models;

namespace ExchangeService.Api.Services;

/// <summary>
/// Interface for external exchange rate API client
/// </summary>
public interface IExchangeRateApiClient
{
    /// <summary>
    /// Gets exchange rates for the specified base currency
    /// </summary>
    /// <param name="baseCurrency">The base currency code (e.g., "AUD")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exchange rate data from the external API</returns>
    Task<ExchangeRateApiResponse> GetExchangeRatesAsync(string baseCurrency, CancellationToken cancellationToken = default);
}