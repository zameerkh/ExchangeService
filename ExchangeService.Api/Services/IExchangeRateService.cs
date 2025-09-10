using ExchangeService.Api.Models;

namespace ExchangeService.Api.Services;

/// <summary>
/// Interface for the core exchange rate business logic
/// </summary>
public interface IExchangeRateService
{
    /// <summary>
    /// Converts an amount from one currency to another
    /// </summary>
    /// <param name="request">The exchange request containing amount and currency details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The exchange response with the converted amount</returns>
    Task<ExchangeResponse> ConvertCurrencyAsync(ExchangeRequest request, CancellationToken cancellationToken = default);
}