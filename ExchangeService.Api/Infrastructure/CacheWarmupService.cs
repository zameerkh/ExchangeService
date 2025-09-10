using ExchangeService.Api.Configuration;
using ExchangeService.Api.Services;
using Microsoft.Extensions.Options;

namespace ExchangeService.Api.Infrastructure;

/// <summary>
/// Background service that warms up the cache on startup and periodically
/// </summary>
public class CacheWarmupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CachingOptions _cachingOptions;
    private readonly ILogger<CacheWarmupService> _logger;
    private readonly TimeSpan _warmupInterval = TimeSpan.FromHours(1); // Warm up every hour

    /// <summary>
    /// Initializes a new instance of the CacheWarmupService
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving scoped services</param>
    /// <param name="cachingOptions">Caching configuration options</param>
    /// <param name="logger">Logger instance</param>
    public CacheWarmupService(
        IServiceProvider serviceProvider,
        IOptions<CachingOptions> cachingOptions,
        ILogger<CacheWarmupService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _cachingOptions = cachingOptions?.Value ?? throw new ArgumentNullException(nameof(cachingOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes the cache warmup service
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    /// <returns>Task representing the background operation</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_cachingOptions.EnableCacheWarming)
        {
            _logger.LogInformation("Cache warming is disabled");
            return;
        }

        _logger.LogInformation("Cache warmup service started");

        // Initial warmup with a small delay to allow the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        await WarmupCacheAsync(stoppingToken);

        // Periodic warmup
        using var timer = new PeriodicTimer(_warmupInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await WarmupCacheAsync(stoppingToken);
        }

        _logger.LogInformation("Cache warmup service stopped");
    }

    private async Task WarmupCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting cache warmup for {CurrencyCount} currencies", _cachingOptions.WarmupCurrencies.Length);

            using var scope = _serviceProvider.CreateScope();
            var cacheClient = scope.ServiceProvider.GetService<HybridCachedExchangeRateApiClient>();
            
            if (cacheClient != null)
            {
                await cacheClient.WarmupCacheAsync(_cachingOptions.WarmupCurrencies, cancellationToken);
                _logger.LogInformation("Cache warmup completed successfully");
            }
            else
            {
                _logger.LogWarning("Could not resolve HybridCachedExchangeRateApiClient for cache warmup");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Cache warmup was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during cache warmup");
        }
    }
}
