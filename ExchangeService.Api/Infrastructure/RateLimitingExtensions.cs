using System.Threading.RateLimiting;

namespace ExchangeService.Api.Infrastructure;

/// <summary>
/// Extension methods for configuring rate limiting
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Adds rate limiting services and policies
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            // Global rate limiter - 100 requests per minute per IP
            // TODO(PROD): For multi-instance deployments, consider a distributed rate limiter or enforce limits at the gateway/CDN.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                httpContext => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:GlobalPermitLimit", 100),
                        Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:GlobalWindowMinutes", 1))
                    }));

            // API-specific rate limiter - more restrictive for exchange endpoint
            options.AddPolicy("ExchangeApi", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:ExchangeApiPermitLimit", 100),
                        Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:ExchangeApiWindowMinutes", 1))
                    }));

            // Authentication rate limiter - generous for login attempts
            options.AddPolicy("AuthApi", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:AuthApiPermitLimit", 50),
                        Window = TimeSpan.FromMinutes(configuration.GetValue<int>("RateLimiting:AuthApiWindowMinutes", 1))
                    }));

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsync(
                    "Too many requests. Please try again later.", cancellationToken: token);
            };
        });

        return services;
    }
}
