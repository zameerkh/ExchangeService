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
