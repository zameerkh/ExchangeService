# Next steps for production

This checklist highlights what’s already production-ready and what to finish before a real launch. Each ❌ includes a short action and where to change it.

## Security
- ✅ JWT validation hardening (RequireSignedTokens, RequireExpirationTime, short ClockSkew) — `Infrastructure/AuthenticationExtensions.cs`
- ✅ Authorization policies (Admin, ReadOnly, ExchangeService, Premium, ApiKey)
- ✅ HSTS + HTTPS redirection — `Program.cs`, `ServiceCollectionExtensions.AddSecurityFeatures`
- ✅ Strict security headers (CSP, COOP/COEP, Permissions-Policy) — `SecurityHeadersMiddleware`
- ✅ Input validation with FluentValidation
- ✅ Swagger guarded in production — `Program.cs` (auth required)
- ❌ Configure explicit CORS origins for production (no "*")
  - Action: Set `CORS:AllowedOrigins` in `appsettings.Production.json` or environment variables.
  - Where: `Program.cs` CORS registration throws if wildcard in prod; supply explicit origins.
- ❌ Secrets management (keys, Redis, tokens) via a vault
  - Action: Move secrets to Azure Key Vault/AWS Secrets Manager; bind via configuration providers.
  - Where: Configuration setup; ensure `Jwt:SecretKey` and `Caching:RedisConnectionString` are not in source.
- ❌ Token revocation/introspection (if using long-lived tokens or reference tokens)
  - Action: Add revocation list/identity provider introspection.
  - Where: `AuthenticationExtensions` and your IdP configuration.

## Reliability & resilience
- ✅ HttpClient resilience: Timeout → Retry (exp+jitter) → Circuit Breaker — `Program.cs`
- ✅ Tail latency bounded (client timeout < policy timeout)
- ✅ External dependency health checks; Redis optional — `ServiceCollectionExtensions.AddProductionHealthChecks`
- ✅ Rate limiting (global + endpoint) — `RateLimitingExtensions`
- ❌ Readiness vs liveness health checks
  - Action: Tag at least one check as `"ready"` (e.g., Redis or external API) so `/health/ready` is meaningful.
  - Where: `AddProductionHealthChecks` — add `tags: new[] { "ready" }` as appropriate.
- ❌ Graceful shutdown tuning
  - Action: Configure `HostOptions.ShutdownTimeout` and ensure long-running tasks honor `CancellationToken`.
  - Where: `Program.cs` builder host options; background services if/when enabled.
- ❌ Distributed rate limiting (multi-instance)
  - Action: Move rate limiting to edge (gateway/CDN) or implement a distributed limiter (e.g., Redis-based) for consistency.
  - Where: `RateLimitingExtensions` and/or platform gateway config.

## Performance
- ✅ Response compression (gzip+brotli) and request decompression — `ServiceCollectionExtensions`
- ✅ Output caching policies (default, ExchangeRates, Health, Static) — `OutputCachingExtensions`
- ✅ ETag support — `ETagMiddleware`
- ✅ Kestrel request limits and JSON depth — `Program.cs`
- ❌ ETag scope tuning
  - Action: Skip ETag for large payloads/non-cacheable content types to avoid double buffering cost.
  - Where: `ETagMiddleware` — add size/content-type checks.
- ❌ Sizing & budgets
  - Action: Define P90/P99 latency and error budgets; alert on SLO violations.
  - Where: Ops/observability configuration and docs.

## Observability
- ✅ OpenTelemetry: ASP.NET Core, HttpClient, runtime metrics — `ServiceCollectionExtensions.AddObservability`
- ✅ Prometheus scraping endpoint — `Program.cs`
- ✅ Correlation middleware and structured request logging — `Program.cs`, `CorrelationMiddleware`
- ❌ OTLP exporter to a collector (traces/metrics/logs)
  - Action: Add OTLP exporters and endpoint config (collector/agent); gate by environment.
  - Where: `AddObservability` — add exporters with configuration keys.
- ❌ Trace/log correlation enrichment
  - Action: Include `TraceId`/`SpanId` in Serilog logs via an enricher.
  - Where: Serilog configuration and logging enrichment.

## Maintainability & testability
- ✅ Unit + integration tests present
- ✅ Health UI restricted in prod
- ❌ CI/CD pipeline
  - Action: Add GitHub Actions (build, test, code scan, publish image, deploy).
- ❌ Analyzers & security scanning
  - Action: Enable Roslyn analyzers/StyleCop; add SAST/dep scanning.
- ❌ Containerization & deployment manifests
  - Action: Add Dockerfile, compose, and optionally Helm/K8s manifests.

## Mappings: where to act
- CORS origins: `ExchangeService.Api/Program.cs` (CORS section) + configuration
- OTLP exporters: `ExchangeService.Api/Infrastructure/ServiceCollectionExtensions.AddObservability`
- Ready checks: `ExchangeService.Api/Infrastructure/ServiceCollectionExtensions.AddProductionHealthChecks`
- Distributed rate limiting: `ExchangeService.Api/Infrastructure/RateLimitingExtensions` or edge
- ETag tuning: `ExchangeService.Api/Infrastructure/OutputCachingExtensions.ETagMiddleware`
- Token revocation: `ExchangeService.Api/Infrastructure/AuthenticationExtensions` + IdP

---

Tip: Use configuration profiles per environment (Development/Production) and keep secrets out of source. See `docs/security.md` and `docs/operations.md` for detailed guidance.