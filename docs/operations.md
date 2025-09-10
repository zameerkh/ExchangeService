# Operations

## Health Endpoints

### `/health/live`
- **Purpose**: Self-check for liveness probe
- **Response**: Always returns 200 with `{"status":"Healthy"}`
- **Use case**: Container orchestration liveness detection

### `/health/ready`
- **Purpose**: Dependency checks for readiness probe  
- **Tags**: Checks marked with "ready" tag (external APIs, cache)
- **Response**: 200 if all dependencies healthy, 503 if any failing
- **Use case**: Load balancer readiness routing

### `/health`
- **Purpose**: Comprehensive health report
- **Includes**: All health checks with detailed status
- **Response**: JSON with individual check results
- **Use case**: Operational dashboards and debugging

## Metrics

### Prometheus Scrape Endpoint
- **Path**: `/metrics`
- **Format**: Prometheus text format
- **Auth restrictions**: Requires `Admin` role in production
- **Includes**: ASP.NET Core, HTTP client, runtime (GC/threadpool) metrics

## Logging & Tracing

### Serilog Configuration
- **Production**: Console sink only (structured JSON)
- **Development**: Console + file sink (`logs/exchangeservice-*.txt`)
- **Rationale**: Avoid file I/O in containerized production environments

### OTLP Exporter Configuration
```json
{
  "OpenTelemetry": {
    "Endpoint": "http://jaeger:14268/api/traces",
    "Headers": {
      "Authorization": "Bearer <token>"
    }
  }
}
```

### Excluded Paths from Tracing
- `/health` - Reduces noise from health check polling
- `/metrics` - Prevents self-monitoring overhead  
- `/api-docs` - Documentation access not business-critical

## Rollout Guidance

### Canary Deployments
Deploy to subset of instances first. Monitor health endpoints, error rates, and latency before full rollout. Use feature flags for gradual feature activation.

### Rollback Strategy  
Keep previous container image available. Monitor circuit breaker state and error rates. Automated rollback triggers: >5% error rate or >3 consecutive health check failures.

### Configuration Changes
Rate limits and cache TTLs can be adjusted via config without deployment:

| Config Key | Purpose | Safe Range |
|------------|---------|------------|
| `RateLimiting:PermitLimit` | Requests per window | 50-500 |
| `RateLimiting:WindowInSeconds` | Rate limit window | 30-300 |  
| `Caching:ExchangeRatesTTLMinutes` | Cache duration | 1-60 |
| `ExchangeRateApi:TimeoutSeconds` | HTTP timeout | 1-10 |

## Runbooks

### TODO: Runbooks
Detailed operational procedures for common scenarios:
- High error rate investigation
- Cache invalidation procedures  
- External API outage response
- Performance degradation debugging

*Link to runbooks will be added when `/runbooks` documentation is available.*

## Deployment

### Environment Configuration

| Environment | Description | Key Settings |
|-------------|-------------|--------------|
| **Development** | Local development | Debug logging, in-memory cache |
| **Staging** | Pre-production testing | Info logging, Redis cache |
| **Production** | Live environment | Warning+ logging, Redis, monitoring |

### Container Deployment

```bash
# Build production image
docker build -t exchange-service .

# Run with environment variables
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ExchangeRateApi__ApiKey="your-key" \
  -e Caching__RedisConnectionString="redis:6379" \
  exchange-service
```

### Load Balancing

The application is stateless and supports:
- **Horizontal scaling** behind load balancers
- **Blue-green deployments**
- **Rolling updates** with health checks

### Performance Considerations

- **Memory**: ~100MB base, +50MB per 1000 concurrent users
- **CPU**: Lightweight, primarily I/O bound
- **Network**: External API calls are cached and rate-limited
