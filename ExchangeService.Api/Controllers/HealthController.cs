using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ExchangeService.Api.Controllers;

/// <summary>
/// Health check controller for monitoring service availability
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    /// <returns>Health status information</returns>
    /// <response code="200">Service is healthy</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), (int)HttpStatusCode.OK)]
    public ActionResult<HealthResponse> Get()
    {
        _logger.LogInformation("Health check requested");
        
        return Ok(new HealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    /// <summary>
    /// Detailed health check with dependencies
    /// </summary>
    /// <returns>Detailed health status</returns>
    /// <response code="200">Service and dependencies are healthy</response>
    /// <response code="503">Service or dependencies are unhealthy</response>
    [HttpGet("detailed")]
    [ProducesResponseType(typeof(DetailedHealthResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(DetailedHealthResponse), (int)HttpStatusCode.ServiceUnavailable)]
    public async Task<ActionResult<DetailedHealthResponse>> GetDetailed()
    {
        _logger.LogInformation("Detailed health check requested");
        
        var response = new DetailedHealthResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            Dependencies = new Dictionary<string, string>()
        };

        // Check external API connectivity (basic check)
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var testResponse = await httpClient.GetAsync("https://api.exchangerate-api.com/v4/latest/USD");
            response.Dependencies["ExchangeRateAPI"] = testResponse.IsSuccessStatusCode ? "Healthy" : "Unhealthy";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for ExchangeRateAPI");
            response.Dependencies["ExchangeRateAPI"] = "Unhealthy";
            response.Status = "Degraded";
        }

        var statusCode = response.Status == "Healthy" ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable;
        return StatusCode((int)statusCode, response);
    }
}

/// <summary>
/// Basic health response model
/// </summary>
public class HealthResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
}

/// <summary>
/// Detailed health response model with dependency status
/// </summary>
public class DetailedHealthResponse : HealthResponse
{
    public Dictionary<string, string> Dependencies { get; set; } = new();
}
