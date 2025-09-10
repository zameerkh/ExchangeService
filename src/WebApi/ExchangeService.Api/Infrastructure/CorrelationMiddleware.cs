using System.Diagnostics;

namespace ExchangeService.Api.Infrastructure;

/// <summary>
/// Middleware to handle correlation IDs for request tracing
/// </summary>
public class CorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the CorrelationMiddleware
    /// </summary>
    /// <param name="next">Next middleware in the pipeline</param>
    /// <param name="logger">Logger instance</param>
    public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task representing the middleware execution</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        // Get or generate correlation ID
        var correlationId = GetOrGenerateCorrelationId(context);
        
        // Set correlation ID in response headers
        if (!context.Response.Headers.ContainsKey("X-Correlation-ID"))
        {
            context.Response.Headers.Append("X-Correlation-ID", correlationId);
        }

        // Propagate W3C trace context if available
        var activity = Activity.Current;
        if (activity != null)
        {
            var traceParent = activity.Id;
            if (!string.IsNullOrEmpty(traceParent) && !context.Response.Headers.ContainsKey("traceparent"))
            {
                context.Response.Headers.Append("traceparent", traceParent);
            }
        }

        // Store correlation ID in HttpContext for later use
        context.Items["CorrelationId"] = correlationId;

        await _next(context);
    }

    /// <summary>
    /// Gets correlation ID from request headers or generates a new one
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Correlation ID</returns>
    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        // Check common correlation ID headers
        var correlationHeaders = new[]
        {
            "X-Correlation-ID",
            "X-Request-ID", 
            "X-Trace-ID",
            "Request-Id"
        };

        foreach (var header in correlationHeaders)
        {
            if (context.Request.Headers.TryGetValue(header, out var value) && 
                !string.IsNullOrEmpty(value))
            {
                return value.ToString();
            }
        }

        // Fall back to TraceIdentifier or generate new GUID
        return !string.IsNullOrEmpty(context.TraceIdentifier) 
            ? context.TraceIdentifier 
            : Guid.NewGuid().ToString();
    }
}
