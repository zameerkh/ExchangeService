using System.ComponentModel.DataAnnotations;

namespace ExchangeService.Api.Models;

/// <summary>
/// Request model for currency exchange operations
/// </summary>
public class ExchangeRequest
{
    /// <summary>
    /// The amount to be converted
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
}