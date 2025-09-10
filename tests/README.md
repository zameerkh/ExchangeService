# Tests

Behavior-focused tests for the Exchange Service.

Suite:
- UnitTests
  - ExchangeRequestValidatorTests: rejects amount <= 0, same currency, invalid codes
  - ExchangeRateServiceTests: correct math/rounding, same-currency short-circuit, missing rate error
  - HybridCachedExchangeRateApiClientTests: miss → populate, hit → no inner call
  - ExchangeRateApiClientTests: JSON parse happy-path; 429 handling
- IntegrationTests
  - ExchangeApiIntegrationTests:
    - POST /api/exchange/convert happy path (fake upstream)
    - Auth: 401 missing token; 403 insufficient scope
    - Caching: second call doesn’t hit upstream
    - Resilience: transient 5xx retried and succeeds
    - Health: /health and /health/live return 200

Run:
- All: dotnet test
- Unit only: dotnet test --filter Category=Unit
- Integration only: dotnet test --filter Category=Integration

Notes:
- No external network; upstream faked via TestHttpMessageHandler
- JWTs minted with JwtTestHelper
- Deterministic; no sleeps
