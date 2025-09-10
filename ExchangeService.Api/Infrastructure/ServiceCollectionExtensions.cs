using ExchangeService.Api.Configuration;
using ExchangeService.Api.Infrastructure;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.ResponseCompression;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.IO.Compression;

namespace ExchangeService.Api.Infrastructure;

/// <summary>
/// Extension methods for configuring production-grade services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds hybrid caching services (Memory + Redis)
    /// </summary>
    public static IServiceCollection AddHybridCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CachingOptions>(configuration.GetSection(CachingOptions.SectionName));
        
        var cachingOptions = configuration.GetSection(CachingOptions.SectionName).Get<CachingOptions>() ?? new CachingOptions();

        if (cachingOptions.UseRedis)
        {
            // Add Redis distributed cache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cachingOptions.RedisConnectionString;
                options.InstanceName = "ExchangeService";
            });
        }

        // Add hybrid cache
        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = cachingOptions.L1CacheSizeLimitMB * 1024 * 1024; // Convert MB to bytes
            options.MaximumKeyLength = 1024;
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(cachingOptions.ExchangeRatesCacheMinutes),
                LocalCacheExpiration = TimeSpan.FromMinutes(cachingOptions.ExchangeRatesCacheMinutes / 2)
            };
        });

        return services;
    }

    /// <summary>
    /// Adds comprehensive health checks
    /// </summary>
    public static IServiceCollection AddProductionHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var healthChecksBuilder = services.AddHealthChecks()
            .AddCheck<ExchangeRateApiHealthCheck>("exchange_rate_api", HealthStatus.Degraded, new[] { "api", "external" })
            .AddCheck("self", () => HealthCheckResult.Healthy("API is running"), new[] { "self" });

    // TODO(PROD): Mark one or more checks as "ready" for the readiness probe, e.g. external API or Redis.
    // Example: .AddCheck<SomeDependency>("dep_name", failureStatus: HealthStatus.Unhealthy, tags: new[] { "ready" })

        var cachingOptions = configuration.GetSection(CachingOptions.SectionName).Get<CachingOptions>();
        if (cachingOptions?.UseRedis == true)
        {
            healthChecksBuilder.AddRedis(
                cachingOptions.RedisConnectionString!,
                name: "redis_cache",
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "cache", "redis" });
        }

        // Add network connectivity check for external API
        var exchangeApiUrl = configuration["ExchangeRateApi:BaseUrl"];
        if (!string.IsNullOrEmpty(exchangeApiUrl))
        {
            healthChecksBuilder.AddCheck(
                "external_api_connectivity",
                new SimpleHealthCheck(exchangeApiUrl),
                failureStatus: HealthStatus.Degraded,
                tags: new[] { "network", "external" });
        }

        // Add Health Checks UI
        services.AddHealthChecksUI(options =>
        {
            options.SetEvaluationTimeInSeconds(30);
            options.MaximumHistoryEntriesPerEndpoint(60);
            options.SetApiMaxActiveRequests(1);
            options.AddHealthCheckEndpoint("API Health", "/health");
        }).AddInMemoryStorage();

        return services;
    }

    /// <summary>
    /// Adds OpenTelemetry observability
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = "ExchangeService.Api";
        var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = environment,
                    ["service.namespace"] = "ExchangeService",
                    ["service.instance.id"] = Environment.MachineName
                }))
            .WithTracing(tracing => tracing
                .AddSource(ExchangeServiceActivitySource.Name) // Add our custom activity source
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    // Exclude noisy endpoints from tracing
                    options.Filter = httpContext => 
                        !httpContext.Request.Path.StartsWithSegments("/health") &&
                        !httpContext.Request.Path.StartsWithSegments("/metrics");
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.FilterHttpRequestMessage = request =>
                        !request.RequestUri?.AbsolutePath.Contains("/health") == true;
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation() // Add HTTP runtime instrumentation (GC, threadpool)
                .AddPrometheusExporter());

    // TODO(PROD): Add OTLP exporters (traces/metrics/logs) to send data to your collector/agent.
    // Configure endpoints via configuration and enable conditionally for non-Dev environments.

        return services;
    }

    /// <summary>
    /// Adds security configurations
    /// </summary>
    public static IServiceCollection AddSecurityFeatures(this IServiceCollection services)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.SuppressXFrameOptionsHeader = false;
        });

        // Add HSTS
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365);
        });

        return services;
    }

    /// <summary>
    /// Adds response compression
    /// </summary>
    public static IServiceCollection AddResponseCompressionFeatures(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
            options.Providers.Add<BrotliCompressionProvider>();
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        return services;
    }
}
