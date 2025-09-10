using System.Diagnostics;

namespace ExchangeService.Api.Infrastructure;

/// <summary>
/// Custom activity source for OpenTelemetry tracing
/// </summary>
public static class ExchangeServiceActivitySource
{
    /// <summary>
    /// Activity source name
    /// </summary>
    public const string Name = "ExchangeService.Api";

    /// <summary>
    /// Activity source instance
    /// </summary>
    public static readonly ActivitySource Instance = new(Name);
}

/// <summary>
/// Extension methods for OpenTelemetry custom tracing
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Creates a new activity for currency conversion operations
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="inputCurrency">Input currency code</param>
    /// <param name="outputCurrency">Output currency code</param>
    /// <param name="amount">Amount to convert</param>
    /// <returns>Activity instance or null if not enabled</returns>
    public static Activity? StartCurrencyConversionActivity(
        string operationName,
        string inputCurrency,
        string outputCurrency,
        decimal amount)
    {
        var activity = ExchangeServiceActivitySource.Instance.StartActivity(operationName);
        
        activity?.SetTag("currency.input", inputCurrency);
        activity?.SetTag("currency.output", outputCurrency);
        activity?.SetTag("currency.amount", amount.ToString());
        activity?.SetTag("operation.type", "currency_conversion");
        
        return activity;
    }

    /// <summary>
    /// Creates a new activity for API client operations
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="apiEndpoint">API endpoint being called</param>
    /// <param name="cacheHit">Whether this was a cache hit</param>
    /// <returns>Activity instance or null if not enabled</returns>
    public static Activity? StartApiClientActivity(
        string operationName,
        string apiEndpoint,
        bool? cacheHit = null)
    {
        var activity = ExchangeServiceActivitySource.Instance.StartActivity(operationName);
        
        activity?.SetTag("api.endpoint", apiEndpoint);
        activity?.SetTag("operation.type", "api_call");
        
        if (cacheHit.HasValue)
        {
            activity?.SetTag("cache.hit", cacheHit.Value);
        }
        
        return activity;
    }

    /// <summary>
    /// Creates a new activity for caching operations
    /// </summary>
    /// <param name="operationName">Name of the operation</param>
    /// <param name="cacheKey">Cache key</param>
    /// <param name="cacheType">Type of cache (memory, redis, etc.)</param>
    /// <returns>Activity instance or null if not enabled</returns>
    public static Activity? StartCacheActivity(
        string operationName,
        string cacheKey,
        string cacheType)
    {
        var activity = ExchangeServiceActivitySource.Instance.StartActivity(operationName);
        
        activity?.SetTag("cache.key", cacheKey);
        activity?.SetTag("cache.type", cacheType);
        activity?.SetTag("operation.type", "cache_operation");
        
        return activity;
    }

    /// <summary>
    /// Adds error information to an activity
    /// </summary>
    /// <param name="activity">Activity to add error to</param>
    /// <param name="exception">Exception that occurred</param>
    public static void RecordError(this Activity? activity, Exception exception)
    {
        if (activity == null) return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag("error.type", exception.GetType().Name);
        activity.SetTag("error.message", exception.Message);
        
        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            activity.SetTag("error.stack_trace", exception.StackTrace);
        }
    }

    /// <summary>
    /// Records business metrics as activity tags
    /// </summary>
    /// <param name="activity">Activity to add metrics to</param>
    /// <param name="exchangeRate">Exchange rate used</param>
    /// <param name="processingTimeMs">Processing time in milliseconds</param>
    /// <param name="dataSource">Source of exchange rate data</param>
    public static void RecordBusinessMetrics(
        this Activity? activity,
        decimal exchangeRate,
        double processingTimeMs,
        string dataSource)
    {
        if (activity == null) return;

        activity.SetTag("business.exchange_rate", exchangeRate.ToString());
        activity.SetTag("business.processing_time_ms", processingTimeMs.ToString());
        activity.SetTag("business.data_source", dataSource);
    }
}
