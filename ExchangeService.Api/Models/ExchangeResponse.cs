using System.ComponentModel.DataAnnotations;

namespace ExchangeService.Api.Models;

/// <summary>
/// Response model for currency exchange operations
/// </summary>
public class ExchangeResponse
{
    /// <summary>
    /// The original amount that was converted
    /// </summary>
    [Required]
    public decimal Amount { get; set; }

    /// <summary>
    /// The source currency code (e.g., "AUD")
    /// </summary>
    [Required]
    public string InputCurrency { get; set; } = string.Empty;

    /// <summary>
    /// The target currency code (e.g., "USD")
    /// </summary>
    [Required]
    public string OutputCurrency { get; set; } = string.Empty;

    /// <summary>
    /// The converted amount in the target currency
    /// </summary>
    [Required]
    public decimal Value { get; set; }

    /// <summary>
    /// The exchange rate used for conversion
    /// </summary>
    [Required]
    public decimal ExchangeRate { get; set; }

    /// <summary>
    /// UTC timestamp when the conversion was performed
    /// </summary>
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Unique request identifier for tracing and debugging
    /// </summary>
    [Required]
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Processing time in milliseconds
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// Indicates whether the data was served from cache
    /// </summary>
    public bool FromCache { get; set; }

    /// <summary>
    /// API version that processed this request
    /// </summary>
    public string ApiVersion { get; set; } = "1.0";
}