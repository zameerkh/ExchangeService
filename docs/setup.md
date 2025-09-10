# Setup & Configuration

## Prerequisites

- **.NET 8 SDK** or later  
- **Docker** (for containerized deployment)

## Configuration

### Important Configuration Keys

| Section | Key | Purpose | Example |
|---------|-----|---------|---------|
| `Jwt` | `SecretKey` | JWT signing key (256-bit min) | `"your-secret-key-here"` |
| `Jwt` | `Issuer`, `Audience` | Token validation | `"ExchangeService"` |
| `CORS` | `AllowedOrigins` | Allowed cross-origin domains | `["https://yourdomain.com"]` |
| `ExchangeRateApi` | `BaseUrl`, `TimeoutSeconds` | External API config | `3` seconds max |
| `OpenTelemetry` | `Endpoint` | OTLP exporter endpoint | `"http://jaeger:14268"` |
| `RateLimiting` | `PermitLimit`, `WindowInSeconds` | Rate limit settings | `100` per `60` seconds |
| `Caching` | `ExchangeRatesTTLMinutes` | Cache duration | `5` minutes |

### Environment Variables

```bash
# Required for production
export JWT__SECRETKEY="your-256-bit-secret-key"
export EXCHANGERATEAPI__APIKEY="your-api-key"

# Optional overrides  
export CORS__ALLOWEDORIGINS__0="https://yourdomain.com"
export RATELIMITING__PERMITLIMIT="200"
export CACHING__EXCHANGERATESTTKMINUTES="10"
```

## Local Development

### .NET Run

```bash
# Clone and build
git clone <repository-url>
cd ExchangeService
dotnet build

# Run API (development profile)
dotnet run --project ExchangeService.Api

# Available at: https://localhost:5001
```

### Docker Compose

```yaml
# docker-compose.yml
services:
  exchangeservice:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - JWT__SECRETKEY=dev-secret-key-min-256-bits
```

```bash
# Run with Docker
docker compose up --build

# Available at: http://localhost:8080
```

## Tests

### Running Tests

```bash
# All tests
dotnet test

# Unit tests only
dotnet test ExchangeService.UnitTests

# Integration tests (may require test containers)
dotnet test ExchangeService.IntegrationTests

# With coverage report
dotnet test --collect:"XPlat Code Coverage"
```

### Test Containers

Integration tests use Testcontainers for:
- **Redis container** for caching tests
- **HTTP mock server** for external API simulation

```bash
# Start Redis using Docker
docker run -d -p 6379:6379 redis:alpine

# Update appsettings.Development.json
{
  "Caching": {
    "UseRedis": true,
    "RedisConnectionString": "localhost:6379"
  }
}

# Run the application
dotnet run
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/ExchangeService.UnitTests/
```

## IDE Setup

### Visual Studio / VS Code

The solution includes:
- **`.editorconfig`** for consistent formatting
- **`Directory.Packages.props`** for centralized package management
- **Launch profiles** in `Properties/launchSettings.json`

### Recommended Extensions

- **C# Dev Kit** (VS Code)
- **REST Client** for testing with `api-requests.http`
