using ExchangeService.Api.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExchangeService.Api.Infrastructure;

/// <summary>
/// Health check for the Exchange Rate API
/// </summary>
public class ExchangeRateApiHealthCheck : IHealthCheck
{
    private readonly IExchangeRateApiClient _exchangeRateApiClient;
    private readonly ILogger<ExchangeRateApiHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the ExchangeRateApiHealthCheck
    /// </summary>
    /// <param name="exchangeRateApiClient">Exchange rate API client</param>
    /// <param name="logger">Logger instance</param>
    public ExchangeRateApiHealthCheck(
        IExchangeRateApiClient exchangeRateApiClient,
        ILogger<ExchangeRateApiHealthCheck> logger)
    {
        _exchangeRateApiClient = exchangeRateApiClient ?? throw new ArgumentNullException(nameof(exchangeRateApiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks the health of the Exchange Rate API
    /// </summary>
    /// <param name="context">Health check context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use a lightweight currency for health check (USD is commonly available)
            var response = await _exchangeRateApiClient.GetExchangeRatesAsync("USD", cancellationToken);
            
            if (response?.Rates != null && response.Rates.Count > 0)
            {
                _logger.LogDebug("Exchange Rate API health check passed");
                return HealthCheckResult.Healthy("Exchange Rate API is responsive", 
                    new Dictionary<string, object>
                    {
                        ["base_currency"] = response.Base,
                        ["rates_count"] = response.Rates.Count,
                        ["last_updated"] = response.Date
                    });
            }

            _logger.LogWarning("Exchange Rate API returned empty response");
            return HealthCheckResult.Degraded("Exchange Rate API returned empty response");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Exchange Rate API health check was cancelled");
            return HealthCheckResult.Unhealthy("Exchange Rate API health check was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exchange Rate API health check failed");
            return HealthCheckResult.Unhealthy("Exchange Rate API is not responsive", ex);
        }
    }
}
