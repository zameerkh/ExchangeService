using ExchangeService.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace ExchangeService.Api.Services;

/// <summary>
/// Cached decorator for ExchangeRateApiClient to reduce API calls
/// </summary>
public class CachedExchangeRateApiClient : IExchangeRateApiClient
{
    private readonly IExchangeRateApiClient _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedExchangeRateApiClient> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5); // Cache for 5 minutes

    public CachedExchangeRateApiClient(
        IExchangeRateApiClient inner,
        IMemoryCache cache,
        ILogger<CachedExchangeRateApiClient> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ExchangeRateApiResponse> GetExchangeRatesAsync(string baseCurrency, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"exchange_rates_{baseCurrency.ToUpperInvariant()}";
        
        if (_cache.TryGetValue(cacheKey, out ExchangeRateApiResponse? cachedResponse) && cachedResponse != null)
        {
            _logger.LogInformation("Retrieved exchange rates for {BaseCurrency} from cache", baseCurrency);
            return cachedResponse;
        }

        _logger.LogInformation("Cache miss for {BaseCurrency}, fetching from API", baseCurrency);
        var response = await _inner.GetExchangeRatesAsync(baseCurrency, cancellationToken);
        
        // Cache the response
        _cache.Set(cacheKey, response, _cacheExpiration);
        _logger.LogInformation("Cached exchange rates for {BaseCurrency} for {ExpirationMinutes} minutes", 
            baseCurrency, _cacheExpiration.TotalMinutes);
        
        return response;
    }
}
