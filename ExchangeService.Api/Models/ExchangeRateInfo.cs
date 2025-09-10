using System.ComponentModel.DataAnnotations;

namespace ExchangeService.Api.Models;

/// <summary>
/// Exchange rate information response model
/// </summary>
public class ExchangeRateInfo
{
    /// <summary>
    /// The source currency code (e.g., "USD")
    /// </summary>
    [Required]
    public string InputCurrency { get; set; } = string.Empty;

    /// <summary>
    /// The target currency code (e.g., "EUR")
    /// </summary>
    [Required]
    public string OutputCurrency { get; set; } = string.Empty;

    /// <summary>
    /// The current exchange rate
    /// </summary>
    [Required]
    public decimal ExchangeRate { get; set; }

    /// <summary>
    /// UTC timestamp when the rate was retrieved
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Whether this rate was retrieved from cache
    /// </summary>
    [Required]
    public bool FromCache { get; set; }

    /// <summary>
    /// Unique request identifier for tracing and debugging
    /// </summary>
    [Required]
    public string RequestId { get; set; } = string.Empty;
}
