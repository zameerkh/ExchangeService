using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ExchangeService.Api.Infrastructure;

/// <summary>
/// Extension methods for configuring output caching
/// </summary>
public static class OutputCachingExtensions
{
    /// <summary>
    /// Adds output caching services with custom policies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddOutputCachingPolicies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOutputCache(options =>
        {
            // Default policy - 5 minutes cache
            options.AddBasePolicy(builder =>
                builder.Expire(TimeSpan.FromMinutes(5))
                       .SetVaryByHeader("Accept", "Accept-Language", "User-Agent")
                       .SetVaryByQuery("*"));

            // Exchange rates policy - cache based on currency pair
            options.AddPolicy("ExchangeRates", builder =>
                builder.Expire(TimeSpan.FromMinutes(1)) // Currency rates change frequently
                       .SetVaryByQuery("inputCurrency", "outputCurrency", "amount")
                       .SetVaryByHeader("Accept", "Accept-Language")
                       .Tag("exchange-rates"));

            // Health checks policy - very short cache
            options.AddPolicy("HealthCheck", builder =>
                builder.Expire(TimeSpan.FromSeconds(30))
                       .SetVaryByHeader("Accept"));

            // Static content policy - longer cache
            options.AddPolicy("Static", builder =>
                builder.Expire(TimeSpan.FromHours(1))
                       .SetVaryByHeader("Accept-Encoding"));
        });

        return services;
    }
}

/// <summary>
/// Middleware for adding ETag support to responses
/// </summary>
public class ETagMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ETagMiddleware> _logger;

    public ETagMiddleware(RequestDelegate next, ILogger<ETagMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply ETag to GET requests
        if (context.Request.Method != HttpMethods.Get)
        {
            await _next(context);
            return;
        }

    // TODO(PROD): Consider skipping ETag for large payloads or non-cacheable content types to avoid double buffering cost.

        // Create a memory stream to capture the response
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        await _next(context);

        // Only add ETag for successful responses
        if (context.Response.StatusCode == 200 && responseBodyStream.Length > 0)
        {
            var responseBody = responseBodyStream.ToArray();
            var etag = GenerateETag(responseBody);

            context.Response.Headers.ETag = etag;

            // Check if client sent If-None-Match header
            if (context.Request.Headers.TryGetValue("If-None-Match", out StringValues ifNoneMatch))
            {
                if (ifNoneMatch.Contains(etag))
                {
                    context.Response.StatusCode = 304; // Not Modified
                    context.Response.Body = originalBodyStream;
                    return;
                }
            }

            // Copy the response body back
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            context.Response.Body = originalBodyStream;
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
        else
        {
            // Copy the response body back for non-cacheable responses
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            context.Response.Body = originalBodyStream;
            await responseBodyStream.CopyToAsync(originalBodyStream);
        }
    }

    private static string GenerateETag(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        var etag = Convert.ToBase64String(hash);
        return $"\"{etag}\"";
    }
}

/// <summary>
/// Extension method to add ETag middleware
/// </summary>
public static class ETagMiddlewareExtensions
{
    /// <summary>
    /// Adds ETag middleware to the pipeline
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <returns>Application builder for chaining</returns>
    public static IApplicationBuilder UseETag(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ETagMiddleware>();
    }
}
