# Best Practices

Production-grade patterns implemented in the Exchange Service for reliability, performance, and security.

## Rate Limiting

- **Applied**: All API endpoints via `UseRateLimiter()` middleware
- **Default limits**: 100 requests per 60-second sliding window per user
- **429 behavior**: Returns `Retry-After` header with reset time
- **Why it matters**: Prevents abuse and ensures fair resource usage

## Hybrid Caching

- **Key strategy**: `{operation}:{currency_pair}:{amount}` format
- **TTLs**: Exchange rates (5min), user data (1hr), config (24hr)
- **Stampede protection**: Single-flight pattern prevents cache stampedes
- **Invalidation**: Write operations trigger cache eviction
- **Why it matters**: Sub-100ms response times with 95%+ cache hit rates

## Resilience via Polly

- **Timeout first**: 1.5s timeout policy before retry/circuit breaker
- **Retry with jitter**: 3 attempts with exponential backoff + randomization
- **Circuit breaker**: Opens after 3 failures, 30s break duration
- **No bulkhead**: Single HttpClient pool (adequate for current scale)
- **Why it matters**: Graceful degradation during external API failures

## JWT Auth + Authorization

- **Scopes/roles**: `exchange:read`, `exchange:write`, `Admin` role
- **Deny-by-default**: All endpoints require authentication unless explicitly public
- **Token validation**: Signed tokens required, 2-minute max clock skew
- **Why it matters**: Zero-trust security model with fine-grained access control

## Security Headers

- **X-Content-Type-Options**: `nosniff`
- **X-Frame-Options**: `DENY`
- **X-XSS-Protection**: `1; mode=block`
- **Referrer-Policy**: `strict-origin-when-cross-origin`
- **CSP**: Production removes `unsafe-inline` (dev allows for tooling)
- **Cross-Origin-Opener-Policy**: `same-origin`
- **Cross-Origin-Embedder-Policy**: `require-corp`
- **Why it matters**: Defense-in-depth against web-based attacks

## CORS Policy

- **Development**: Allows `*` origins for rapid iteration
- **Production**: Explicit allow-list from `CORS:AllowedOrigins` config
- **Credentials rule**: No `AllowCredentials` with wildcard origins
- **Why it matters**: Prevents unauthorized cross-origin API access

## Health Checks

- **`/health/live`**: Self-check, always returns 200 (liveness probe)
- **`/health/ready`**: Dependency checks tagged as "ready" (readiness probe)
- **`/health`**: Full health report with all dependencies
- **Why it matters**: Enables proper container orchestration and load balancing

## Observability

- **Serilog JSON**: Structured logging to console (production), file (dev)
- **OpenTelemetry traces**: W3C format with correlation ID propagation
- **Metrics**: Prometheus endpoint with ASP.NET Core + HTTP client metrics
- **Excluded paths**: `/health`, `/metrics`, `/api-docs` filtered from traces
- **Why it matters**: Production debugging and performance monitoring

## Output Caching & ETags

- **When used**: GET endpoints with `[OutputCache]` attribute
- **ETag generation**: SHA256 hash of response content
- **Conditional GETs**: `If-None-Match` returns 304 when unchanged
- **Why it matters**: Reduces bandwidth and server load for repeated requests

## Config Validation

- **Options binding**: All config sections bound to strongly-typed classes
- **ValidateOnStart**: Startup fails fast if configuration is invalid
- **DataAnnotations**: Required fields, ranges, formats validated
- **Why it matters**: Prevents runtime failures from configuration errors

### Authentication & Authorization
- **JWT Bearer token** validation with proper claims
- **Role-based access control** (Admin, Premium, Regular)
- **Token expiration** and refresh handling
- **Secure token storage** recommendations

### Security Headers
- **HSTS** (HTTP Strict Transport Security)
- **X-Content-Type-Options**: nosniff
- **X-Frame-Options**: DENY
- **Content Security Policy** for XSS prevention
- **Referrer-Policy**: strict-origin-when-cross-origin

### Input Validation
- **FluentValidation** for complex business rules
- **Data annotations** for simple validation
- **Model binding security** with proper type checking
- **Anti-forgery tokens** for state-changing operations

### Rate Limiting
- **Per-user rate limits** based on authentication
- **Global rate limits** for anonymous users
- **Sliding window** algorithm for fair distribution
- **Rate limit headers** for client awareness

### CORS Policy
- **Explicit origin whitelisting** (no wildcards in production)
- **Credential handling** configuration
- **Preflight caching** for performance
- **Method and header restrictions**

## Observability

### Structured Logging
- **Serilog** with structured JSON output
- **Correlation IDs** for request tracing
- **Performance logging** for slow operations
- **Security event logging** (failed auth, rate limits)

### Metrics & Monitoring
- **OpenTelemetry** instrumentation
- **Prometheus metrics** export
- **Custom business metrics** (conversion rates, API usage)
- **Resource utilization** monitoring (CPU, memory, connections)

### Distributed Tracing
- **Request correlation** across service boundaries
- **External API call tracing** with timing
- **Database operation spans**
- **Cache operation tracking**

## Configuration Management

### Environment-Specific Config
- **appsettings.json** hierarchy (base → environment → user secrets)
- **Environment variable** overrides
- **Configuration validation** on startup
- **Sensitive data** in Azure Key Vault or environment variables

### Feature Flags
- **Runtime configuration** changes without deployment
- **A/B testing** capabilities
- **Gradual rollout** of new features
- **Emergency switches** for quick feature disabling

## Error Handling

### Global Exception Handling
- **Middleware-based** exception catching
- **Consistent error responses** with problem details (RFC 7807)
- **Error correlation** with request IDs
- **Sensitive information** filtering from responses

### Graceful Degradation
- **Fallback mechanisms** when external services fail
- **Cached data serving** during API outages
- **Partial functionality** maintenance during degraded states
- **User-friendly error messages**

## Code Quality

### Clean Architecture
- **Dependency inversion** principle
- **Separation of concerns** across layers
- **Domain-driven design** with rich models
- **Testable architecture** with proper abstractions

### Central Package Management
- **Directory.Packages.props** for version consistency
- **Security vulnerability** scanning
- **Automated dependency** updates with testing
- **License compliance** checking

### Testing Strategy
- **Unit tests** for business logic (80%+ coverage)
- **Integration tests** for API endpoints
- **Architecture tests** for dependency rules
- **Performance tests** for critical paths

## Deployment & Operations

### Containerization
- **Multi-stage Docker** builds for optimization
- **Non-root user** execution for security
- **Health check** integration
- **Resource limits** and requests

### Infrastructure as Code
- **Docker Compose** for local development
- **Kubernetes manifests** for production deployment
- **Helm charts** for configuration management
- **Environment promotion** pipelines

### Monitoring & Alerting
- **SLA/SLO definitions** for key metrics
- **Automated alerting** for threshold breaches
- **Runbook automation** for common issues
- **Capacity planning** based on usage trends

## Development Practices

### CI/CD Pipeline
- **Automated testing** on every commit
- **Security scanning** (SAST/DAST)
- **Dependency vulnerability** checks
- **Automated deployment** to staging/production

### Code Standards
- **EditorConfig** for consistent formatting
- **Static analysis** with code quality gates
- **Pre-commit hooks** for validation
- **Architectural decision records** (ADRs)
