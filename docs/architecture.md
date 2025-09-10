# Architecture

Exchange Service implements Clean Architecture with production-grade middleware pipeline and request tracing.

## System Layers

```
Controllers/APIs ──→ Services ──→ HttpClient (Typed) ──→ External APIs
       │                │              │                    │
       │                │              │                    ├─ Exchange Rate API
       │                │              │                    └─ Future: Currency DB
       │                │              │
       │                │              └─ Polly Policies (Timeout → Retry → Circuit Breaker)
       │                │
       │                └─ IExchangeRateApiClient (Decorator Pattern)
       │                   ├─ ExchangeRateApiClient (Base)
       │                   └─ HybridCachedExchangeRateApiClient (Decorator)
       │
       └─ Dependencies: Cache (Memory/Redis), Config, Validators
```

## Request Flow with Correlation

```
[Request] ──→ [Correlation ID] ──→ [Rate Limit] ──→ [Auth] ──→ [Controller]
                     │                                              │
                     ├─ Response Headers (X-Correlation-ID)        │
                     ├─ OpenTelemetry Span Context                 │
                     └─ W3C traceparent propagation                │
                                                                   │
[Response] ←── [ETag] ←── [Output Cache] ←── [Service Layer] ←────┘
```

## Decorator Pattern for Caching

The `IExchangeRateApiClient` interface uses the decorator pattern:

- **Base**: `ExchangeRateApiClient` - Direct HTTP calls with Polly resilience
- **Decorator**: `HybridCachedExchangeRateApiClient` - Adds caching layer
- **Registration**: DI container injects cached decorator as `IExchangeRateApiClient`

## Middleware Pipeline Order

Production middleware pipeline (order matters):

1. **GlobalExceptionMiddleware** - Catch unhandled exceptions
2. **CorrelationMiddleware** - Generate/propagate correlation IDs  
3. **UseRequestDecompression** - Handle gzipped request bodies
4. **UseRateLimiter** - Apply rate limiting early
5. **UseHttpsRedirection** - Force HTTPS
6. **UseHsts** - HTTP Strict Transport Security (production only)
7. **SecurityHeadersMiddleware** - Security headers (CSP, X-Frame-Options, etc.)
8. **UseSerilogRequestLogging** - Structured request logging
9. **UseRouting** - Route resolution
10. **UseCors** - Cross-origin request handling
11. **UseAuthentication** - JWT token validation
12. **UseAuthorization** - Policy-based authorization
13. **UseResponseCompression** - Compress outbound responses
14. **UseOutputCache** - HTTP response caching
15. **UseETag** - Conditional request support
                         │              │
                    (Interfaces)   (Pure Logic)
```

- **Domain**: Pure business logic, no dependencies
- **Application**: Use cases and interfaces
- **Infrastructure**: External services, data access, caching
- **API**: Controllers, middleware, configuration

## Project Structure

```
src/
# Architecture

Clean Architecture with a production-grade middleware pipeline, typed HttpClient, and correlation/trace propagation.

## Layers (overview)

- API (controllers, middleware, configuration)
- Application/Services (use cases, validation)
- Infrastructure (external clients, caching, configuration)
- Domain (entities, value objects, pure logic)

## Project structure (repo)

```
.
├── ExchangeService.sln
├── README.md
├── docs/
├── Directory.Packages.props
├── Dockerfile
├── docker-compose.yml
├── deploy.ps1 / deploy.sh
├── ExchangeService.Api/
│   ├── Controllers/
│   ├── Infrastructure/            # Middleware & extensions (security, rate limiting, etc.)
│   ├── Models/
│   ├── Services/                  # API-layer service adapters
│   └── Program.cs
├── src/
│   ├── Domain/                    # Pure domain: entities, value objects
│   ├── Application/               # Use cases, interfaces (e.g., IExchangeRateService)
│   └── Infrastructure/            # External impls (cache, options, DI wiring)
└── tests/
    ├── ExchangeService.UnitTests/
    └── ExchangeService.IntegrationTests/
```

## System map

```
Controllers/APIs ──→ Services ──→ HttpClient (Typed) ──→ External APIs
       │                │              │                    │
       │                │              │                    ├─ Exchange Rate API
       │                │              │                    └─ Future: Currency DB
       │                │              │
       │                │              └─ Polly (Timeout → Retry → Circuit Breaker)
       │                │
       │                └─ IExchangeRateApiClient (Decorator)
       │                   ├─ ExchangeRateApiClient (Base)
       │                   └─ HybridCachedExchangeRateApiClient (Cache)
       │
       └─ Dependencies: Cache (Memory/Redis), Config, Validators
```

## Request flow (correlation & caching)

```
[Request] → [Correlation ID] → [Rate Limit] → [Auth] → [Controller]
    │              │               │                │
    │              ├─ Response header: X-Correlation-ID
    │              ├─ W3C traceparent propagation
    │              └─ OpenTelemetry spans/attributes
    │
[Response] ← ETag/304 ← OutputCache ← Service (IExchangeRateApiClient → Cache → HTTP)
```

## Middleware pipeline (production order)

1) GlobalExceptionMiddleware
2) CorrelationMiddleware
3) UseRequestDecompression
4) UseRateLimiter
5) UseHttpsRedirection
6) UseHsts (non-Development)
7) SecurityHeadersMiddleware
8) UseSerilogRequestLogging
9) UseRouting
10) UseCors
11) UseAuthentication
12) UseAuthorization
13) UseResponseCompression
14) UseOutputCache
15) UseETag

## Decorator pattern (IExchangeRateApiClient)

- Base: `ExchangeRateApiClient` (typed HttpClient + Polly resilience)
- Decorator: `HybridCachedExchangeRateApiClient` (hybrid cache, stampede protection)
- DI: Decorator registered as `IExchangeRateApiClient`

## Notes
