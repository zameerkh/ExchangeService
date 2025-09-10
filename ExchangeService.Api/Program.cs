using FluentValidation;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;
using System.Reflection;
using ExchangeService.Api.Configuration;
using ExchangeService.Api.Services;
using ExchangeService.Api.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel limits for production
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1_048_576; // 1MB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    options.Limits.MaxConcurrentConnections = 1000;
    options.Limits.MaxConcurrentUpgradedConnections = 100;
});

// Configure JSON options to limit depth
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.MaxDepth = 10; // Limit JSON depth
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

// Configure Serilog
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console();

// Add file sink only in Development (avoid file writes in containerized production)
if (builder.Environment.IsDevelopment())
{
    loggerConfig.WriteTo.File("logs/exchangeservice-.txt", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        restrictedToMinimumLevel: LogEventLevel.Information);
}

Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Add configuration validation
builder.Services.AddOptions<ExchangeRateApiOptions>()
    .Bind(builder.Configuration.GetSection(ExchangeRateApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CachingOptions>()
    .Bind(builder.Configuration.GetSection(CachingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// Add Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Exchange Service API", 
        Version = "v1",
        Description = "A robust currency exchange API with hybrid caching, monitoring, and production-grade features.",
        Contact = new() { Name = "Exchange Service Team", Email = "support@exchangeservice.com" },
        License = new() { Name = "MIT License", Url = new Uri("https://opensource.org/licenses/MIT") }
    });
    
    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add JWT Authentication and Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();

// Add Output Caching
builder.Services.AddOutputCachingPolicies(builder.Configuration);

// Configure HTTP clients with Polly policies
var exchangeRateConfig = builder.Configuration.GetSection(ExchangeRateApiOptions.SectionName);
var retryAttempts = exchangeRateConfig.GetValue<int>("RetryAttempts", 3);
var retryDelaySeconds = exchangeRateConfig.GetValue<int>("RetryDelaySeconds", 1);
var retryJitterMaxSeconds = exchangeRateConfig.GetValue<int>("RetryJitterMaxSeconds", 2);
var circuitBreakerFailureThreshold = exchangeRateConfig.GetValue<int>("CircuitBreakerFailureThreshold", 3);
var circuitBreakerDurationSeconds = exchangeRateConfig.GetValue<int>("CircuitBreakerDurationSeconds", 30);

// Create a random instance for jitter
var random = new Random();

builder.Services.AddHttpClient<ExchangeRateApiClient>("ExchangeRateApi", client =>
{
    var baseUrl = builder.Configuration["ExchangeRateApi:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        client.BaseAddress = new Uri(baseUrl);
    }
    // Reduce timeout so Polly controls tail latency
    client.Timeout = TimeSpan.FromSeconds(3);
})
.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(1.5))) // Timeout policy first
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(
        retryCount: retryAttempts,
        sleepDurationProvider: retryAttempt =>
        {
            var baseDelay = retryDelaySeconds * Math.Pow(2, retryAttempt - 1);
            var jitter = random.NextDouble() * retryJitterMaxSeconds;
            return TimeSpan.FromSeconds(baseDelay + jitter);
        },
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            Log.Warning("ExchangeRateApiClient retry {RetryCount} in {Delay}ms",
                retryCount, timespan.TotalMilliseconds);
        }))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: circuitBreakerFailureThreshold,
        durationOfBreak: TimeSpan.FromSeconds(circuitBreakerDurationSeconds),
        onBreak: (exception, duration) =>
        {
            Log.Warning("ExchangeRateApiClient circuit breaker opened for {Duration}ms", duration.TotalMilliseconds);
        },
        onReset: () =>
        {
            Log.Information("ExchangeRateApiClient circuit breaker reset");
        }));

// Add enhanced CORS with configuration
var corsConfig = builder.Configuration.GetSection("CORS");
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = corsConfig.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "*" };
        var allowedMethods = corsConfig.GetSection("AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST" };
        var allowedHeaders = corsConfig.GetSection("AllowedHeaders").Get<string[]>() ?? new[] { "*" };
        var exposedHeaders = corsConfig.GetSection("ExposedHeaders").Get<string[]>() ?? Array.Empty<string>();
        var allowCredentials = corsConfig.GetValue<bool>("AllowCredentials", false);
        var maxAge = corsConfig.GetValue<int>("MaxAge", 300);

        // Production CORS hardening
        if (builder.Environment.IsDevelopment())
        {
            // Development: Allow wildcard origins
            if (allowedOrigins.Contains("*"))
            {
                policy.AllowAnyOrigin();
            }
            else
            {
                policy.WithOrigins(allowedOrigins);
            }
        }
        else
        {
            // Production: Require explicit origins from config
            if (allowedOrigins.Contains("*"))
            {
                throw new InvalidOperationException("Wildcard origins (*) not allowed in production. Configure explicit origins.");
            }
            policy.WithOrigins(allowedOrigins);
        }

        policy.WithMethods(allowedMethods)
              .WithHeaders(allowedHeaders)
              .WithExposedHeaders(exposedHeaders)
              .SetPreflightMaxAge(TimeSpan.FromSeconds(maxAge));

        // Prevent AllowCredentials with wildcard origins
        if (allowCredentials && !allowedOrigins.Contains("*"))
        {
            policy.AllowCredentials();
        }
        else if (allowCredentials && allowedOrigins.Contains("*"))
        {
            throw new InvalidOperationException("Cannot use AllowCredentials with wildcard origins (*).");
        }
    });
});

// Add hybrid caching (Memory + Redis)
builder.Services.AddHybridCaching(builder.Configuration);

// Add response compression
builder.Services.AddResponseCompressionFeatures();

// Add request decompression for gzipped request bodies
builder.Services.AddRequestDecompression();

// Add security features
builder.Services.AddSecurityFeatures();

// Add rate limiting
builder.Services.AddRateLimiting(builder.Configuration);

// Add health checks
builder.Services.AddProductionHealthChecks(builder.Configuration);

// Add observability (OpenTelemetry)
builder.Services.AddObservability(builder.Configuration);

// Register custom activity source for OpenTelemetry
builder.Services.AddSingleton(ExchangeServiceActivitySource.Instance);

// Register application services
builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();

// Remove the duplicate registration - typed HttpClient is the only registration needed
// builder.Services.AddScoped<ExchangeRateApiClient>(); // REMOVED: Duplicate registration

// Register the hybrid cached decorator
builder.Services.AddScoped<HybridCachedExchangeRateApiClient>();
builder.Services.AddScoped<IExchangeRateApiClient>(provider =>
{
    var baseClient = provider.GetRequiredService<ExchangeRateApiClient>();
    var hybridCache = provider.GetRequiredService<Microsoft.Extensions.Caching.Hybrid.HybridCache>();
    var logger = provider.GetRequiredService<ILogger<HybridCachedExchangeRateApiClient>>();
    var cachingOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CachingOptions>>();
    return new HybridCachedExchangeRateApiClient(baseClient, hybridCache, logger, cachingOptions);
});

// Add background service for cache warming
// Temporarily disabled to troubleshoot HttpClient configuration
// builder.Services.AddHostedService<CacheWarmupService>();

var app = builder.Build();

// Configure the HTTP request pipeline in proper order for production

// 1. Global exception handling (catch all unhandled exceptions)
app.UseMiddleware<GlobalExceptionMiddleware>();

// 2. Correlation ID middleware for request tracing
app.UseMiddleware<CorrelationMiddleware>();

// 3. Request decompression (handle gzipped request bodies)
app.UseRequestDecompression();

// 4. Rate limiting (prevent abuse early)
app.UseRateLimiter();

// 5. HTTPS redirection (redirect HTTP to HTTPS)
app.UseHttpsRedirection();

// 6. HSTS (HTTP Strict Transport Security) in production
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// 7. Security headers (before any response is generated)
app.UseMiddleware<SecurityHeadersMiddleware>();

// 8. Request logging (log all requests)
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) => ex != null
        ? LogEventLevel.Error
        : httpContext.Response.StatusCode > 499
            ? LogEventLevel.Error
            : LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
        diagnosticContext.Set("UserId", httpContext.User.Identity?.Name);
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
    };
});

// 9. Routing (required for endpoints)
app.UseRouting();

// 10. CORS (Cross-Origin Resource Sharing)
app.UseCors();

// 11. Authentication (who are you?)
app.UseAuthentication();

// 12. Authorization (what can you do?)
app.UseAuthorization();

// 13. Response compression (reduce response size)
app.UseResponseCompression();

// 14. Output caching (cache responses)
app.UseOutputCache();

// 15. ETag support (conditional requests)
app.UseETag();

// 16. Swagger (API documentation) - secured in production
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Exchange Service API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
        c.EnableDeepLinking();
        c.DisplayRequestDuration();
        c.EnableFilter();
        c.ShowExtensions();
        c.EnableValidator();
    });
}
else
{
    // Production: Restrict Swagger to authenticated users
    app.UseWhen(context => context.Request.Path.StartsWithSegments("/swagger") || 
                          context.Request.Path.StartsWithSegments("/api-docs"), 
        appBuilder =>
        {
            appBuilder.UseAuthentication();
            appBuilder.UseAuthorization();
            appBuilder.Use(async (context, next) =>
            {
                if (!context.User.Identity?.IsAuthenticated == true)
                {
                    context.Response.StatusCode = 401;
                    return;
                }
                await next();
            });
        });
    
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Exchange Service API v1");
        c.RoutePrefix = "api-docs";
        c.DocumentTitle = "Exchange Service API Documentation";
    });
}

// Add health check endpoints with security restrictions
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Self health check that always returns 200 quickly
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false, // No actual health checks, just return healthy
    ResponseWriter = async (context, _) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"status\":\"Healthy\"}");
    }
});

// Secure health checks UI in production
if (app.Environment.IsDevelopment())
{
    app.MapHealthChecksUI(options =>
    {
        options.UIPath = "/health-ui";
        options.ApiPath = "/health-ui-api";
    });
}
else
{
    // Production: Restrict health UI to authenticated users
    app.MapHealthChecksUI(options =>
    {
        options.UIPath = "/health-ui";
        options.ApiPath = "/health-ui-api";
    }).RequireAuthorization("Admin");
}

// Secure Prometheus metrics endpoint in production
if (app.Environment.IsDevelopment())
{
    app.MapPrometheusScrapingEndpoint();
}
else
{
    app.MapPrometheusScrapingEndpoint().RequireAuthorization("Admin");
}

app.MapControllers();

try
{
    Log.Information("Starting Exchange Service API v{Version}", 
        Assembly.GetExecutingAssembly().GetName().Version);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Entry point class for the Exchange Service API
/// </summary>
public partial class Program { }