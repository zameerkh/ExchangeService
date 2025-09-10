using System.Text.Json.Serialization;

namespace ExchangeService.Api.Models;

/// <summary>
/// Response model for the external Exchange Rate API
/// Based on the structure from https://api.exchangerate-api.com/v4/latest/{base_currency}
/// </summary>
public class ExchangeRateApiResponse
{
    /// <summary>
    /// The base currency for the exchange rates
    /// </summary>
    [JsonPropertyName("base")]
    public string Base { get; set; } = string.Empty;

    /// <summary>
    /// The date when the rates were last updated
    /// </summary>
    [JsonPropertyName("date")]
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Dictionary of currency codes and their exchange rates relative to the base currency
    /// </summary>
    [JsonPropertyName("rates")]
    public Dictionary<string, decimal> Rates { get; set; } = new();

    /// <summary>
    /// Gets the exchange rate for a specific currency
    /// </summary>
    /// <param name="currencyCode">The currency code to get the rate for</param>
    /// <returns>The exchange rate, or null if not found</returns>
    public decimal? GetRate(string currencyCode)
    {
        return Rates.TryGetValue(currencyCode.ToUpperInvariant(), out var rate) ? rate : null;
    }
}