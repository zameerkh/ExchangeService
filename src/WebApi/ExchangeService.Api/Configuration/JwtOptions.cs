using System.ComponentModel.DataAnnotations;

namespace ExchangeService.Api.Configuration;

/// <summary>
/// Configuration options for JWT authentication
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// JWT secret key for signing tokens
    /// </summary>
    [Required(ErrorMessage = "JWT SecretKey is required")]
    [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters")]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// JWT token issuer
    /// </summary>
    [Required(ErrorMessage = "JWT Issuer is required")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// JWT token audience
    /// </summary>
    [Required(ErrorMessage = "JWT Audience is required")]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    [Range(1, 43200, ErrorMessage = "JWT ExpiryMinutes must be between 1 and 43200 (30 days)")]
    public int ExpiryMinutes { get; set; } = 60;

    /// <summary>
    /// Whether to require HTTPS for JWT tokens
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Whether to validate the token issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Whether to validate the token audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Whether to validate the token lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Clock skew allowance in minutes
    /// </summary>
    [Range(0, 10, ErrorMessage = "ClockSkewMinutes must be between 0 and 10")]
    public int ClockSkewMinutes { get; set; } = 5;
}
