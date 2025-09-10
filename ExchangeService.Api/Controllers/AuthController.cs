using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ExchangeService.Api.Configuration;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.RateLimiting;

namespace ExchangeService.Api.Controllers;

/// <summary>
/// Authentication controller for JWT token management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the AuthController
    /// </summary>
    /// <param name="jwtOptions">JWT configuration options</param>
    /// <param name="logger">Logger instance</param>
    public AuthController(IOptions<JwtOptions> jwtOptions, ILogger<AuthController> logger)
    {
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="request">Login request</param>
    /// <returns>JWT token and user information</returns>
    /// <response code="200">Authentication successful</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Invalid credentials</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost("login")]
    [EnableRateLimiting("AuthApi")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 429)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        using var activity = Infrastructure.TelemetryExtensions.StartCurrencyConversionActivity(
            "auth.login", "N/A", "N/A", 0);

        // In a real application, validate against a user store
        // This is a simplified example for demonstration
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            _logger.LogWarning("Login attempt with missing credentials");
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Username and password are required",
                Status = 400
            });
        }

        // Simulate user validation (replace with real authentication)
        var user = await ValidateUserAsync(request.Username, request.Password);
        if (user == null)
        {
            _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
            return Unauthorized(new ProblemDetails
            {
                Title = "Authentication Failed",
                Detail = "Invalid username or password",
                Status = 401
            });
        }

        var token = GenerateJwtToken(user);
        
        _logger.LogInformation("Successful login for user: {Username}", user.Username);
        
        activity?.SetTag("auth.username", user.Username);
        activity?.SetTag("auth.success", "true");

        return Ok(new LoginResponse
        {
            Token = token,
            Username = user.Username,
            Roles = user.Roles,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes)
        });
    }

    /// <summary>
    /// Gets current user information from JWT token
    /// </summary>
    /// <returns>Current user information</returns>
    /// <response code="200">User information retrieved successfully</response>
    /// <response code="401">Invalid or missing token</response>
    [HttpGet("me")]
    [Authorize(Policy = "AuthenticatedUser")]
    [ProducesResponseType(typeof(UserInfo), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public ActionResult<UserInfo> GetCurrentUser()
    {
        var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        var scopes = User.FindAll("scope").Select(c => c.Value).ToArray();

        return Ok(new UserInfo
        {
            Username = username ?? "Unknown",
            Roles = roles,
            Scopes = scopes
        });
    }

    private async Task<UserModel?> ValidateUserAsync(string username, string password)
    {
        // Simulate async user validation
        await Task.Delay(100); // Simulate database lookup

        // Demo users - replace with real user store
        var demoUsers = new Dictionary<string, UserModel>
        {
            ["admin"] = new() { Username = "admin", Roles = ["Admin"], Scopes = ["exchange:read", "exchange:write"] },
            ["user"] = new() { Username = "user", Roles = ["User"], Scopes = ["exchange:read"] },
            ["premium"] = new() { Username = "premium", Roles = ["User"], Scopes = ["exchange:read", "exchange:write"], Subscription = "premium" }
        };

        if (demoUsers.TryGetValue(username, out var user) && password == "demo123")
        {
            return user;
        }

        return null;
    }

    private string GenerateJwtToken(UserModel user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Username),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, 
                new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), 
                ClaimValueTypes.Integer64)
        };

        // Add role claims
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Add scope claims
        claims.AddRange(user.Scopes.Select(scope => new Claim("scope", scope)));

        // Add subscription claim if present
        if (!string.IsNullOrEmpty(user.Subscription))
        {
            claims.Add(new Claim("subscription", user.Subscription));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Login request model
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username for authentication
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Login response model
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Username of authenticated user
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User roles
    /// </summary>
    public string[] Roles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// User information model
/// </summary>
public class UserInfo
{
    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User roles
    /// </summary>
    public string[] Roles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// User scopes/permissions
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Internal user model
/// </summary>
internal class UserModel
{
    public string Username { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public string? Subscription { get; set; }
}
