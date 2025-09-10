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
├── Domain/                     # Core business entities
│   ├── Entities/
│   │   └── ExchangeRate.cs     # Exchange rate business entity
│   └── ValueObjects/
│       ├── Currency.cs         # Currency value object
│       └── Money.cs            # Money value object
│
├── Application/                # Business logic and interfaces
│   └── Common/
│       └── Interfaces/
│           ├── ICacheService.cs          # Caching abstraction
│           └── IExchangeRateService.cs   # Exchange rate contract
│
├── Infrastructure/             # External service implementations
│   ├── Configuration/
│   │   └── ExchangeRateApiOptions.cs    # API configuration
│   ├── Services/
│   │   └── HybridCacheService.cs        # Caching implementation
│   └── DependencyInjection.cs           # Service registration
│
└── ExchangeService.Api/        # Web API layer
    ├── Controllers/
    │   ├── ExchangeController.cs        # Currency conversion endpoints
    │   └── HealthController.cs          # Health check endpoints
    ├── Infrastructure/
    │   └── GlobalExceptionMiddleware.cs  # Error handling
    ├── Models/
    │   ├── ExchangeRequest.cs           # Request DTOs
    │   ├── ExchangeResponse.cs          # Response DTOs
    │   └── ExchangeRequestValidator.cs  # Input validation
    └── Program.cs                       # Application bootstrap
```

## Design Patterns

### Clean Architecture
- **Dependency inversion** - outer layers depend on inner layers
- **Separation of concerns** - each layer has specific responsibilities
- **Testability** - business logic isolated from infrastructure

### Domain-Driven Design
- **Value objects** (Currency, Money) for type safety
- **Entities** (ExchangeRate) with business behavior
- **Rich domain models** with encapsulated logic

### CQRS (Planned)
- **Commands** for state-changing operations
- **Queries** for data retrieval
- **Separation** of read and write models

## Technology Stack

### Core Framework
- **.NET 8** - Latest LTS runtime
- **ASP.NET Core** - Web API framework
- **C# 12** - Latest language features

### External Dependencies
- **Serilog** - Structured logging
- **FluentValidation** - Input validation
- **OpenTelemetry** - Observability
- **Polly** - Resilience patterns
- **Redis** - Distributed caching (optional)

### Development Tools
- **Central Package Management** - Consistent versioning
- **EditorConfig** - Code formatting
- **Docker** - Containerization
- **Swagger/OpenAPI** - API documentation
