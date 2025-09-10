# Exchange Service

Production-grade .NET 8 Web API for currency exchange rates with hybrid caching, resilience patterns, and comprehensive observability.

## Quick Start

```bash
# Clone and build
git clone <repository-url>
cd ExchangeService
dotnet build

# Run locally
dotnet run --project ExchangeService.Api
```

## Testing API Endpoints

### Option 1: Visual Studio (Built-in HTTP Support)
1. Open the solution in **Visual Studio 2022+**
2. Navigate to `src/WebApi/ExchangeService.Api/ExchangeService.Api.http`
3. Click the **green play button** (▶️) next to each HTTP request
4. Run requests in sequence: Login → Extract token → Use authenticated endpoints
5. View responses in the integrated HTTP response window

### Option 2: VS Code REST Client Extension
1. Install the **REST Client** extension (by Huachao Mao): `humao.rest-client`
2. Open `src/WebApi/ExchangeService.Api/ExchangeService.Api.http`
3. Click "Send Request" above each HTTP request section

### Option 3: Manual Tools (alternative options)
Use **Thunder Client** (VS Code extension), **Postman**, or other HTTP clients:

1. **Login to get token:**
   ```
   POST https://localhost:49898/api/auth/login
   Content-Type: application/json
   
   {
     "username": "premium", 
     "password": "demo123"
   }
   ```

2. **Copy the token from response** and use in subsequent requests:
   ```
   GET https://localhost:49898/api/exchange/rates?inputCurrency=USD&outputCurrency=AUD
   Authorization: Bearer <your-token-here>
   ```

### Option 4: Command Line (curl)
```bash
# Get token
TOKEN=$(curl -s -X POST https://localhost:49898/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"premium","password":"demo123"}' | \
  jq -r '.token')

# Use token for API calls
curl -H "Authorization: Bearer $TOKEN" \
  "https://localhost:49898/api/exchange/rates?inputCurrency=USD&outputCurrency=AUD"
```

## Authentication

API requires JWT authentication. Example request:

```bash
curl -H "Authorization: Bearer <your-jwt-token>" \
     https://localhost:5001/api/exchange?from=USD&to=EUR&amount=100
```

## Documentation

- **[Architecture](docs/architecture.md)** - System design and request flow
- **[Best Practices](docs/best-practices.md)** - Production patterns and guidelines  
- **[Setup](docs/setup.md)** - Configuration and local development
- **[Operations](docs/operations.md)** - Monitoring, health checks, and deployment
- **[Security](docs/security.md)** - Authentication, authorization, and security headers
- **[Contributing](docs/contributing.md)** - Development workflow and standards

- 🚀 New: **[Next steps for production](docs/next-steps-for-production.md)** — targeted checklist of gaps to close before go-live

API documentation available at `/swagger` (development) or `/api-docs` (production).

Dev helpers:
- **HTTP Collection**: `src/WebApi/ExchangeService.Api/ExchangeService.Api.http` — Complete request flow with login, auth/me, convert, rates (with ETag), health, swagger json, metrics. Works with Visual Studio 2022+ built-in HTTP support or VS Code REST Client extension.
