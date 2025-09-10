using ExchangeService.Api.Models;
using ExchangeService.Api.Infrastructure;

namespace ExchangeService.Api.Services;

/// <summary>
/// Core business logic service for currency exchange operations
/// </summary>
public class ExchangeRateService : IExchangeRateService
{
    private readonly IExchangeRateApiClient _apiClient;
    private readonly ILogger<ExchangeRateService> _logger;

    /// <summary>
    /// Initializes a new instance of the ExchangeRateService
    /// </summary>
    /// <param name="apiClient">The exchange rate API client</param>
    /// <param name="logger">The logger instance</param>
    public ExchangeRateService(
        IExchangeRateApiClient apiClient,
        ILogger<ExchangeRateService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Converts an amount from one currency to another using live exchange rates
    /// </summary>
    /// <param name="request">The exchange request containing amount and currency details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The exchange response with the converted amount</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when exchange rate is not available</exception>
    /// <exception cref="HttpRequestException">Thrown when external API call fails</exception>
    public async Task<ExchangeResponse> ConvertCurrencyAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var inputCurrency = request.InputCurrency.ToUpperInvariant();
        var outputCurrency = request.OutputCurrency.ToUpperInvariant();

        using var activity = TelemetryExtensions.StartCurrencyConversionActivity(
            "exchange_rate_service.convert_currency",
            inputCurrency,
            outputCurrency,
            request.Amount);

        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();

        _logger.LogInformation("Converting {Amount} {InputCurrency} to {OutputCurrency} [RequestId: {RequestId}]", 
            request.Amount, inputCurrency, outputCurrency, requestId);

        try
        {
            // Handle same currency conversion (should not happen due to validation, but defensive programming)
            if (inputCurrency == outputCurrency)
            {
                _logger.LogInformation("Same currency conversion requested, returning original amount [RequestId: {RequestId}]", requestId);
                stopwatch.Stop();
                return new ExchangeResponse
                {
                    Amount = request.Amount,
                    InputCurrency = inputCurrency,
                    OutputCurrency = outputCurrency,
                    Value = request.Amount,
                    ExchangeRate = 1.0m,
                    Timestamp = startTime,
                    RequestId = requestId,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    FromCache = true, // Same currency is always "cached"
                    ApiVersion = "1.0"
                };
            }

            // Get exchange rates using the input currency as base
            // This approach gets rates relative to the input currency
            var beforeApiCall = stopwatch.ElapsedMilliseconds;
            var exchangeRateData = await _apiClient.GetExchangeRatesAsync(inputCurrency, cancellationToken);
            var afterApiCall = stopwatch.ElapsedMilliseconds;

            // Determine if response came from cache (this is a simplified check)
            var fromCache = (afterApiCall - beforeApiCall) < 50; // If response was very fast, likely from cache

            // Get the rate for the output currency
            var exchangeRate = exchangeRateData.GetRate(outputCurrency);

            if (exchangeRate == null)
            {
                _logger.LogError("Exchange rate not found for currency pair {InputCurrency}/{OutputCurrency} [RequestId: {RequestId}]", 
                    inputCurrency, outputCurrency, requestId);
                throw new InvalidOperationException(
                    $"Exchange rate not available for currency pair {inputCurrency}/{outputCurrency}");
            }

            // Calculate the converted amount
            // Since we're using input currency as base, we multiply by the rate
            var convertedAmount = request.Amount * exchangeRate.Value;

            // Round to 2 decimal places for currency precision
            // Production note: Different currencies may require different precision levels
            var roundedAmount = Math.Round(convertedAmount, 2, MidpointRounding.AwayFromZero);

            stopwatch.Stop();

            _logger.LogInformation("Conversion successful: {Amount} {InputCurrency} = {ConvertedAmount} {OutputCurrency} (rate: {ExchangeRate}) [RequestId: {RequestId}] [FromCache: {FromCache}] [ProcessingTime: {ProcessingTimeMs}ms]", 
                request.Amount, inputCurrency, roundedAmount, outputCurrency, exchangeRate.Value, requestId, fromCache, stopwatch.ElapsedMilliseconds);

            return new ExchangeResponse
            {
                Amount = request.Amount,
                InputCurrency = inputCurrency,
                OutputCurrency = outputCurrency,
                Value = roundedAmount,
                ExchangeRate = exchangeRate.Value,
                Timestamp = startTime,
                RequestId = requestId,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                FromCache = fromCache,
                ApiVersion = "1.0"
            };
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to retrieve exchange rates from external API [RequestId: {RequestId}] [ProcessingTime: {ProcessingTimeMs}ms]", 
                requestId, stopwatch.ElapsedMilliseconds);
            
            // Map specific HTTP exceptions to more user-friendly messages
            var message = ex.Data.Contains("StatusCode") ? ex.Data["StatusCode"]?.ToString() switch
            {
                "TooManyRequests" => "Service is temporarily unavailable due to rate limiting. Please try again later.",
                "ServiceUnavailable" => "Exchange rate service is temporarily unavailable. Please try again later.",
                "NotFound" => $"One or both currencies ({inputCurrency}, {outputCurrency}) are not supported.",
                _ => "Unable to retrieve current exchange rates. Please try again later."
            } : "Unable to retrieve current exchange rates. Please try again later.";

            throw new InvalidOperationException(message, ex);
        }
        catch (InvalidOperationException)
        {
            // Re-throw our own exceptions
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during currency conversion [RequestId: {RequestId}] [ProcessingTime: {ProcessingTimeMs}ms]", 
                requestId, stopwatch.ElapsedMilliseconds);
            throw new InvalidOperationException("An unexpected error occurred during currency conversion", ex);
        }
    }
}