using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExchangeService.Api.Infrastructure;

/// <summary>
/// Simple health check for HTTP endpoints
/// </summary>
public class SimpleHealthCheck : IHealthCheck
{
    private readonly string _url;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the SimpleHealthCheck
    /// </summary>
    /// <param name="url">URL to check</param>
    public SimpleHealthCheck(string url)
    {
        _url = url ?? throw new ArgumentNullException(nameof(url));
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    /// <summary>
    /// Performs the health check
    /// </summary>
    /// <param name="context">Health check context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check result</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(_url, cancellationToken);
            return response.IsSuccessStatusCode 
                ? HealthCheckResult.Healthy("External API is accessible")
                : HealthCheckResult.Degraded($"External API returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("External API is not accessible", ex);
        }
    }

    /// <summary>
    /// Disposes the HTTP client
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
