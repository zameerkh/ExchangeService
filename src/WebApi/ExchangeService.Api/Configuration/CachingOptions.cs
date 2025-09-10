using System.ComponentModel.DataAnnotations;

namespace ExchangeService.Api.Configuration;

/// <summary>
/// Configuration options for caching with validation
/// </summary>
public class CachingOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Caching";

    /// <summary>
    /// Redis connection string for distributed caching
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Exchange rates cache duration in minutes
    /// </summary>
    [Range(1, 1440, ErrorMessage = "ExchangeRatesCacheMinutes must be between 1 and 1440 (24 hours)")]
    public int ExchangeRatesCacheMinutes { get; set; } = 5;

    /// <summary>
    /// Maximum number of cache entries to keep in memory
    /// </summary>
    [Range(100, 100000, ErrorMessage = "MaxCacheEntries must be between 100 and 100000")]
    public int MaxCacheEntries { get; set; } = 1000;

    /// <summary>
    /// L1 (memory) cache size limit in MB
    /// </summary>
    [Range(10, 1000, ErrorMessage = "L1CacheSizeLimitMB must be between 10 and 1000")]
    public int L1CacheSizeLimitMB { get; set; } = 100;

    /// <summary>
    /// L2 (Redis) cache expiration in minutes
    /// </summary>
    [Range(5, 2880, ErrorMessage = "L2CacheExpirationMinutes must be between 5 and 2880 (48 hours)")]
    public int L2CacheExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Whether to enable cache warming on startup
    /// </summary>
    public bool EnableCacheWarming { get; set; } = true;

    /// <summary>
    /// Currencies to warm up cache for
    /// </summary>
    public string[] WarmupCurrencies { get; set; } = { "USD", "EUR", "GBP", "JPY", "AUD", "CAD", "CHF", "CNY" };

    /// <summary>
    /// Whether Redis is enabled for distributed caching
    /// </summary>
    public bool UseRedis => !string.IsNullOrWhiteSpace(RedisConnectionString);
}
