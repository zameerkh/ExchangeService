using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ExchangeService.Tests.TestUtilities;

public static class JwtTestHelper
{
    public static string CreateToken(
        string issuer = "https://test-issuer",
        string audience = "exchange-api",
        string secret = "insecure-test-secret-key-please-replace",
        IEnumerable<Claim>? claims = null,
        TimeSpan? lifetime = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims ?? Array.Empty<Claim>(),
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromHours(1)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
