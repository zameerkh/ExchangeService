using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ExchangeService.Api.Configuration;
using System.Security.Claims;

namespace ExchangeService.Api.Infrastructure;

/// <summary>
/// Extension methods for configuring authentication and authorization
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds JWT authentication and authorization services
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Add and validate JWT options
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing");

        var key = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = jwtOptions.ValidateIssuer,
                ValidateAudience = jwtOptions.ValidateAudience,
                ValidateLifetime = jwtOptions.ValidateLifetime,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true,  // Explicitly require expiration time
                RequireSignedTokens = true,    // Explicitly require signed tokens
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(Math.Min(jwtOptions.ClockSkewMinutes, 2)) // Limit to max 2 minutes
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerHandler>>();
                    logger.LogWarning("JWT authentication failed: {Error} for user: {User}", 
                        context.Exception.Message, 
                        context.HttpContext.User.Identity?.Name ?? "Anonymous");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerHandler>>();
                    var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    logger.LogDebug("JWT token validated for user: {UserId}", userId);
                    // TODO(PROD): If using token revocation, check token against revocation list here or enable IdP introspection.
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Adds authorization policies
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Basic authenticated user policy
            options.AddPolicy("AuthenticatedUser", policy =>
                policy.RequireAuthenticatedUser());

            // Admin policy - requires admin role
            options.AddPolicy("Admin", policy =>
                policy.RequireRole("Admin"));

            // Exchange service policy - requires specific claims
            options.AddPolicy("ExchangeService", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("scope", "exchange:read", "exchange:write"));

            // Read-only policy
            options.AddPolicy("ReadOnly", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("scope", "exchange:read"));

            // Premium user policy - for higher rate limits
            options.AddPolicy("Premium", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("subscription", "premium", "enterprise"));

            // API key policy for service-to-service communication
            options.AddPolicy("ApiKey", policy =>
                policy.RequireClaim("api_key"));
        });

        return services;
    }
}
