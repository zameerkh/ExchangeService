# Contributing

## Development Workflow

### Branching Convention
- **`main`**: Production-ready releases
- **`feature/description`**: New features from main
- **`fix/description`**: Bug fixes from main  
- **`hotfix/description`**: Critical production fixes

### Commit Style
```
feat: add rate limiting middleware
fix: resolve JWT validation edge case
docs: update API examples in README
test: add integration tests for caching
```

Use conventional commits: `type: description` (lowercase, no period).

## Code Style

### Standards Enforced
- **Nullable reference types**: Enabled project-wide
- **.NET analyzers**: Warnings treated as errors in CI
- **StyleCop**: Enforced via `Directory.Build.props`
- **EditorConfig**: 4-space indentation, UTF-8, CRLF normalized

### Key Rules
- **XML comments**: Required for all public APIs (Swagger dependency)
- **Async/await**: Use `ConfigureAwait(false)` in libraries
- **Logging**: Structured logging with meaningful context
- **Exceptions**: Use `ProblemDetails` for API errors

## Test Pyramid

### Unit Tests (Fast)
- **Scope**: Single class/method isolation
- **Mocking**: All external dependencies
- **Coverage**: >80% line coverage required
- **Performance**: <1s total execution time

### Integration Tests (Medium)  
- **Scope**: Component interactions
- **Test containers**: Redis, HTTP mocks
- **Real dependencies**: Cache, HTTP clients
- **Performance**: <30s total execution time

### End-to-End Tests (Slow)
- **Scope**: Full API workflows
- **Real services**: Actual external APIs (limited)
- **Authentication**: Real JWT token flows
- **Performance**: <5min total execution time

## Linting & Security Scans

### Local Development
```bash
# Run analyzers and build
dotnet build --verbosity normal

# Security analysis (if configured)
dotnet list package --vulnerable
dotnet audit

# Code formatting check
dotnet format --verify-no-changes
```

### CI Pipeline Targets
- **SAST**: Static analysis for security vulnerabilities
- **SCA**: Dependency vulnerability scanning  
- **Container scan**: Docker image security analysis
- **License compliance**: OSS license validation

## PR Checklist

### Before Submitting
- [ ] **Builds successfully** without warnings
- [ ] **All tests pass** (unit + integration)
- [ ] **No secrets exposed** in code or commit history
- [ ] **Documentation updated** if behavior/API changes
- [ ] **Breaking changes** documented in PR description
- [ ] **Performance impact** assessed for critical paths

### Review Criteria
- [ ] **Code follows standards** (analyzers pass)
- [ ] **Tests cover new functionality** adequately  
- [ ] **Security implications** reviewed
- [ ] **Observability** (logging/metrics) appropriate
- [ ] **Error handling** follows established patterns
public async Task ConvertCurrency_ValidInput_ReturnsCorrectAmount()

[Test]
public async Task ConvertCurrency_InvalidCurrency_ThrowsValidationException()
```

### Coverage Requirements

- **Unit tests**: Minimum 80% code coverage
- **Integration tests**: Critical business flows
- **All public APIs**: Must have test coverage

## Build & CI

### Local Build

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Check formatting
dotnet format --verify-no-changes
```

### Continuous Integration

The CI pipeline runs on every PR:

1. **Build** all projects
2. **Run** unit and integration tests
3. **Check** code formatting and style
4. **Scan** for security vulnerabilities
5. **Generate** test coverage reports

### Pre-commit Hooks

Recommended pre-commit setup:

```bash
# Install pre-commit tools
dotnet tool install -g dotnet-format
dotnet tool install -g dotnet-outdated-tool

# Pre-commit script
#!/bin/bash
dotnet format --verify-no-changes
dotnet test --no-build
```

## Package Management

### Central Package Management

All package versions are managed in `Directory.Packages.props`:

```xml
<PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
```

### Adding Dependencies

1. **Add version** to `Directory.Packages.props`
2. **Reference package** in project file (no version)
3. **Update documentation** if it affects setup/configuration

### Security Updates

- **Monitor** security advisories for used packages
- **Update** promptly when vulnerabilities are found
- **Test thoroughly** after security updates

## Release Process

### Version Numbering

Follows **Semantic Versioning** (SemVer):

- **Major**: Breaking changes
- **Minor**: New features, backward compatible
- **Patch**: Bug fixes, backward compatible

### Release Checklist

- [ ] All tests passing
- [ ] Documentation updated
- [ ] Security scan clean
- [ ] Performance benchmarks acceptable
- [ ] Deployment tested in staging
- [ ] Release notes prepared
