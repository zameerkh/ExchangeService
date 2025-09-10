using ExchangeService.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace ExchangeService.Infrastructure.Services;

/// <summary>
/// Implementation of ICacheService using HybridCache
/// </summary>
public class HybridCacheService : ICacheService
{
    private readonly HybridCache _hybridCache;

    public HybridCacheService(HybridCache hybridCache)
    {
        _hybridCache = hybridCache ?? throw new ArgumentNullException(nameof(hybridCache));
    }

    /// <summary>
    /// Gets a cached value by key
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            return await _hybridCache.GetOrCreateAsync<T>(key, async (ct) => 
            {
                await Task.CompletedTask;
                return null!;
            }, 
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(1) }, 
            cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets a cached value with optional expiration
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? TimeSpan.FromMinutes(5)
        };

        await _hybridCache.SetAsync(key, value, options, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Removes a cached value by key
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _hybridCache.RemoveAsync(key, cancellationToken);
    }

    /// <summary>
    /// Gets or creates a cached value using the provided factory
    /// </summary>
    public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = expiration ?? TimeSpan.FromMinutes(5)
        };

        return await _hybridCache.GetOrCreateAsync(
            key, 
            async (ct) => await factory(ct), 
            options, 
            cancellationToken: cancellationToken);
    }
}
