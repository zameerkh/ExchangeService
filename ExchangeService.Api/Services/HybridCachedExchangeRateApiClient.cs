using ExchangeService.Api.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using ExchangeService.Api.Configuration;

namespace ExchangeService.Api.Services;

/// <summary>
/// Hybrid cached decorator for ExchangeRateApiClient using L1 (memory) + L2 (Redis) caching
/// </summary>
public class HybridCachedExchangeRateApiClient : IExchangeRateApiClient
{
    private readonly IExchangeRateApiClient _inner;
    private readonly HybridCache _hybridCache;
    private readonly ILogger<HybridCachedExchangeRateApiClient> _logger;
    private readonly CachingOptions _cachingOptions;

    /// <summary>
    /// Initializes a new instance of the HybridCachedExchangeRateApiClient
    /// </summary>
    /// <param name="inner">The inner exchange rate API client</param>
    /// <param name="hybridCache">The hybrid cache instance</param>
    /// <param name="logger">The logger instance</param>
    /// <param name="cachingOptions">The caching configuration options</param>
    public HybridCachedExchangeRateApiClient(
        IExchangeRateApiClient inner,
        HybridCache hybridCache,
        ILogger<HybridCachedExchangeRateApiClient> logger,
        IOptions<CachingOptions> cachingOptions)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cachingOptions = cachingOptions?.Value ?? throw new ArgumentNullException(nameof(cachingOptions));
    }

    /// <summary>
    /// Gets exchange rates for the specified base currency with hybrid caching
    /// </summary>
    /// <param name="baseCurrency">The base currency to get exchange rates for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exchange rate response</returns>
    public async Task<ExchangeRateApiResponse> GetExchangeRatesAsync(string baseCurrency, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"exchange_rates_{baseCurrency.ToUpperInvariant()}";
        
        _logger.LogDebug("Attempting to retrieve exchange rates for {BaseCurrency} from cache", baseCurrency);

        try
        {
            var cacheExpiration = TimeSpan.FromMinutes(_cachingOptions.ExchangeRatesCacheMinutes);
            
            var response = await _hybridCache.GetOrCreateAsync(
                cacheKey,
                async (ct) =>
                {
                    _logger.LogInformation("Cache miss for {BaseCurrency}, fetching from API", baseCurrency);
                    var apiResponse = await _inner.GetExchangeRatesAsync(baseCurrency, ct);
                    _logger.LogInformation("Fetched and cached exchange rates for {BaseCurrency}", baseCurrency);
                    return apiResponse;
                },
                options: new HybridCacheEntryOptions
                {
                    Expiration = cacheExpiration,
                    LocalCacheExpiration = TimeSpan.FromMinutes(_cachingOptions.ExchangeRatesCacheMinutes / 2) // L1 expires sooner
                },
                cancellationToken: cancellationToken);

            _logger.LogDebug("Retrieved exchange rates for {BaseCurrency} from hybrid cache", baseCurrency);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving cached exchange rates for {BaseCurrency}", baseCurrency);
            
            // Fallback to direct API call if cache fails
            _logger.LogInformation("Falling back to direct API call for {BaseCurrency}", baseCurrency);
            return await _inner.GetExchangeRatesAsync(baseCurrency, cancellationToken);
        }
    }

    /// <summary>
    /// Warms up the cache for specified currencies
    /// </summary>
    public async Task WarmupCacheAsync(IEnumerable<string> currencies, CancellationToken cancellationToken = default)
    {
        var warmupTasks = currencies.Select(async currency =>
        {
            try
            {
                _logger.LogInformation("Warming up cache for currency: {Currency}", currency);
                await GetExchangeRatesAsync(currency, cancellationToken);
                _logger.LogInformation("Cache warmed up successfully for currency: {Currency}", currency);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to warm up cache for currency: {Currency}", currency);
            }
        });

        await Task.WhenAll(warmupTasks);
        _logger.LogInformation("Cache warmup completed for {CurrencyCount} currencies", currencies.Count());
    }
}
