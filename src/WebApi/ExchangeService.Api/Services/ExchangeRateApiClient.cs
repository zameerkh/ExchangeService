using ExchangeService.Api.Models;
using Microsoft.Extensions.Options;
using ExchangeService.Api.Configuration;
using System.Text.Json;

namespace ExchangeService.Api.Services;

/// <summary>
/// HTTP client for integrating with the external Exchange Rate API
/// </summary>
public class ExchangeRateApiClient : IExchangeRateApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ExchangeRateApiOptions _options;
    private readonly ILogger<ExchangeRateApiClient> _logger;

    public ExchangeRateApiClient(
        HttpClient httpClient,
        IOptions<ExchangeRateApiOptions> options,
        ILogger<ExchangeRateApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets exchange rates for the specified base currency from exchangerate-api.com
    /// </summary>
    /// <param name="baseCurrency">The base currency code (e.g., "AUD")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exchange rate data from the external API</returns>
    /// <exception cref="HttpRequestException">Thrown when the API request fails</exception>
    /// <exception cref="JsonException">Thrown when the API response cannot be parsed</exception>
    public async Task<ExchangeRateApiResponse> GetExchangeRatesAsync(string baseCurrency, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(baseCurrency))
        {
            throw new ArgumentException("Base currency cannot be null or empty", nameof(baseCurrency));
        }

        var normalizedCurrency = baseCurrency.ToUpperInvariant();
        _logger.LogInformation("Fetching exchange rates for base currency: {BaseCurrency}", normalizedCurrency);

        try
        {
            // Build the request URL - using the free tier endpoint
            // Format: https://api.exchangerate-api.com/v4/latest/{base_currency}
            var requestUri = _httpClient.BaseAddress != null 
                ? $"{normalizedCurrency}" 
                : $"{_options.BaseUrl}{normalizedCurrency}";
            
            _logger.LogDebug("Making request to: {RequestUri}", requestUri);

            var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("API request failed with status {StatusCode}: {ErrorContent}", 
                    response.StatusCode, errorContent);

                // Map HTTP status codes to appropriate exceptions
                throw response.StatusCode switch
                {
                    System.Net.HttpStatusCode.TooManyRequests => new HttpRequestException(
                        "Rate limit exceeded. Please try again later.", null, response.StatusCode),
                    System.Net.HttpStatusCode.NotFound => new HttpRequestException(
                        $"Currency '{normalizedCurrency}' not found or not supported.", null, response.StatusCode),
                    System.Net.HttpStatusCode.ServiceUnavailable => new HttpRequestException(
                        "Exchange rate service is temporarily unavailable.", null, response.StatusCode),
                    _ => new HttpRequestException(
                        $"External API request failed with status {response.StatusCode}", null, response.StatusCode)
                };
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Received response: {JsonContent}", jsonContent);

            var exchangeRateData = JsonSerializer.Deserialize<ExchangeRateApiResponse>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (exchangeRateData == null)
            {
                _logger.LogError("Failed to deserialize API response");
                throw new JsonException("Failed to deserialize exchange rate API response");
            }

            _logger.LogInformation("Successfully retrieved {RateCount} exchange rates for {BaseCurrency}", 
                exchangeRateData.Rates.Count, normalizedCurrency);

            return exchangeRateData;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Request to exchange rate API timed out");
            throw new HttpRequestException("Request to exchange rate API timed out", ex);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Request to exchange rate API was cancelled");
            throw;
        }
        catch (HttpRequestException)
        {
            // Re-throw HTTP exceptions as-is
            throw;
        }
        catch (JsonException)
        {
            // Re-throw JSON exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching exchange rates");
            throw new HttpRequestException("An unexpected error occurred while fetching exchange rates", ex);
        }
    }
}