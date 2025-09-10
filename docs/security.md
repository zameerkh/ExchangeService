# Security

## JWT Validation Parameters

### Token Validation Settings

- **ValidateIssuer**: `true` - Ensures token from trusted issuer
- **ValidateAudience**: `true` - Validates intended audience
- **ValidateLifetime**: `true` - Checks expiration and not-before claims
- **ClockSkew**: Max 2 minutes - Allows minimal time drift
- **RequireExpirationTime**: `true` - Expiration claim mandatory
- **RequireSignedTokens**: `true` - Only signed tokens accepted

### Configuration Example

```json
{
  "Jwt": {
    "SecretKey": "your-256-bit-secret-key-here",
    "Issuer": "ExchangeService", 
    "Audience": "ExchangeService-Users",
    "ClockSkewMinutes": 2,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true
  }
}
```

## CORS Rules

### Development vs Production

| Environment | Allowed Origins | Credentials | Behavior |
|-------------|----------------|-------------|----------|
| Development | `*` (wildcard) | `false` | Permissive for tooling |
| Production | Explicit list from config | `false` | Restrictive security |

### Credential Rule

- **Cannot combine** `AllowCredentials: true` with wildcard origins `*`
- **Production enforcement**: Throws `InvalidOperationException` on startup
- **Security rationale**: Prevents credential exposure to arbitrary origins

## Protected Surfaces in Production

### Restricted Endpoints

| Endpoint | Access Control | Rationale |
|----------|---------------|-----------|
| `/api-docs` | Requires authentication | Prevents API discovery |
| `/health-ui` | Requires `Admin` role | Sensitive system information |
| `/metrics` | Requires `Admin` role | Performance/security metrics |

### Public Endpoints

- `/health/live` - Liveness probe (no auth required)
- `/health/ready` - Readiness probe (no auth required)  
- `/health` - Basic health status (no auth required)

## Secrets Handling

### Environment Variables Only

- **JWT signing keys**: Store in `JWT__SECRETKEY` environment variable
- **API keys**: Use `EXCHANGERATEAPI__APIKEY` pattern
- **Connection strings**: Environment variables or Azure Key Vault
- **No hardcoded secrets**: Static analysis enforced

### Azure Key Vault Integration

```json
{
  "KeyVault": {
    "Endpoint": "https://vault.vault.azure.net/",
    "TenantId": "tenant-id",
    "ClientId": "client-id"
  }
}
```

## Data Privacy

### Logging Practices

- **No PII in logs**: Personal data excluded from structured logs
- **Trace IDs only**: `ProblemDetails` includes correlation ID for debugging
- **Sanitized payloads**: Request/response logging scrubs sensitive fields
- **Log levels**: Error details in Warning+ levels only

### Error Response Format

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Validation Error",
  "status": 400,
  "traceId": "0HN4:00000001"
}
```

**Note**: `traceId` enables correlation without exposing user data.
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

## Security Headers

### Automatic Headers

The application automatically adds security headers:

```http
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
Content-Security-Policy: default-src 'self'
```

### HTTPS Enforcement

- **HSTS** (HTTP Strict Transport Security) enabled
- **Automatic HTTPS redirection** in production
- **Secure cookies** for authentication

## Rate Limiting

### Policies

| User Type | Requests/Minute | Burst Limit |
|-----------|-----------------|-------------|
| **Anonymous** | 10 | 20 |
| **Regular** | 100 | 150 |
| **Premium** | 500 | 750 |
| **Admin** | Unlimited | Unlimited |

### Configuration

```json
{
  "RateLimiting": {
    "GlobalPolicy": {
      "PermitLimit": 100,
      "Window": "00:01:00",
      "QueueLimit": 10
    },
    "PremiumPolicy": {
      "PermitLimit": 500,
      "Window": "00:01:00"
    }
  }
}
```

## Input Validation

### Request Validation

- **Model binding** with data annotations
- **FluentValidation** for complex business rules
- **Anti-forgery tokens** for state-changing operations
- **Input sanitization** for all user inputs

### Example Validation

```csharp
public class ExchangeRequest
{
    [Required]
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string InputCurrency { get; set; }

    [Required] 
    [StringLength(3, MinimumLength = 3)]
    public string OutputCurrency { get; set; }
}
```

## Security Best Practices

### Code Security

- **No secrets in source code** - use configuration/environment variables
- **Input validation** on all endpoints
- **Output encoding** to prevent XSS
- **SQL injection prevention** through parameterized queries

### Infrastructure Security

- **Run as non-root** in containers
- **Minimal container images** (Alpine-based)
- **Network isolation** between services
- **Regular security updates** for dependencies

### Monitoring

- **Failed authentication attempts** logging
- **Rate limit violations** alerting  
- **Unusual access patterns** detection
- **Security headers** compliance monitoring
