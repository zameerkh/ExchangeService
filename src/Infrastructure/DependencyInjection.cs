using ExchangeService.Application.Common.Interfaces;
using ExchangeService.Infrastructure.Configuration;
using ExchangeService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExchangeService.Infrastructure;

/// <summary>
/// Dependency injection configuration for Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<ExchangeRateApiOptions>(configuration.GetSection(ExchangeRateApiOptions.SectionName));

        // Caching
        services.AddHybridCache();
        services.AddScoped<ICacheService, HybridCacheService>();

        return services;
    }
}
