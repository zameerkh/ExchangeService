namespace ExchangeService.Api.Infrastructure;

/// <summary>
/// Middleware to add security headers to HTTP responses
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the SecurityHeadersMiddleware
    /// </summary>
    /// <param name="next">Next middleware in the pipeline</param>
    /// <param name="logger">Logger instance</param>
    public SecurityHeadersMiddleware(RequestDelegate next, ILogger<SecurityHeadersMiddleware> logger)
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
        // Add security headers
        var response = context.Response;
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        
        if (!response.Headers.ContainsKey("X-Content-Type-Options"))
        {
            response.Headers.Append("X-Content-Type-Options", "nosniff");
        }
        
        if (!response.Headers.ContainsKey("X-Frame-Options"))
        {
            response.Headers.Append("X-Frame-Options", "DENY");
        }
        
        if (!response.Headers.ContainsKey("X-XSS-Protection"))
        {
            response.Headers.Append("X-XSS-Protection", "1; mode=block");
        }
        
        if (!response.Headers.ContainsKey("Referrer-Policy"))
        {
            response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        }
        
        if (!response.Headers.ContainsKey("Content-Security-Policy"))
        {
            var csp = isDevelopment 
                ? "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'"
                : "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'";
            
            response.Headers.Append("Content-Security-Policy", csp);
        }
        
        if (!response.Headers.ContainsKey("Permissions-Policy"))
        {
            response.Headers.Append("Permissions-Policy", 
                "camera=(), microphone=(), geolocation=(), payment=(), usb=(), bluetooth=()");
        }

        // Add new security headers
        if (!response.Headers.ContainsKey("Cross-Origin-Opener-Policy"))
        {
            response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
        }

        if (!response.Headers.ContainsKey("Cross-Origin-Embedder-Policy"))
        {
            response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
        }

        // Remove server header for security
        response.Headers.Remove("Server");

        await _next(context);
    }
}
