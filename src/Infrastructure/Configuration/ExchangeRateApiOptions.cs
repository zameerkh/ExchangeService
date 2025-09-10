using System.ComponentModel.DataAnnotations;

namespace ExchangeService.Infrastructure.Configuration;

/// <summary>
/// Configuration options for the Exchange Rate API integration with validation
/// </summary>
public class ExchangeRateApiOptions
{
    public const string SectionName = "ExchangeRateApi";

    /// <summary>
    /// Base URL for the Exchange Rate API
    /// </summary>
    [Required(ErrorMessage = "BaseUrl is required")]
    [Url(ErrorMessage = "BaseUrl must be a valid URL")]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key for authenticated requests (if required)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    [Range(1, 300, ErrorMessage = "TimeoutSeconds must be between 1 and 300")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for failed requests
    /// </summary>
    [Range(0, 10, ErrorMessage = "RetryAttempts must be between 0 and 10")]
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts in seconds
    /// </summary>
    [Range(1, 60, ErrorMessage = "RetryDelaySeconds must be between 1 and 60")]
    public int RetryDelaySeconds { get; set; } = 1;

    /// <summary>
    /// Maximum jitter to add to retry delays in seconds
    /// </summary>
    [Range(0, 30, ErrorMessage = "RetryJitterMaxSeconds must be between 0 and 30")]
    public int RetryJitterMaxSeconds { get; set; } = 2;

    /// <summary>
    /// Number of failures before circuit breaker opens
    /// </summary>
    [Range(1, 20, ErrorMessage = "CircuitBreakerFailureThreshold must be between 1 and 20")]
    public int CircuitBreakerFailureThreshold { get; set; } = 3;

    /// <summary>
    /// Duration in seconds to keep circuit breaker open
    /// </summary>
    [Range(10, 300, ErrorMessage = "CircuitBreakerDurationSeconds must be between 10 and 300")]
    public int CircuitBreakerDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Sampling duration in seconds for circuit breaker
    /// </summary>
    [Range(30, 600, ErrorMessage = "CircuitBreakerSamplingDurationSeconds must be between 30 and 600")]
    public int CircuitBreakerSamplingDurationSeconds { get; set; } = 60;
}
