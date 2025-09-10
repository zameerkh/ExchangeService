using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using ExchangeService.Tests.TestUtilities;
using FluentAssertions;
using Xunit;

namespace ExchangeService.Tests.IntegrationTests;

[Trait("Category", "Integration")]
public class ExchangeApiIntegrationTests
{
    [Fact]
    public async Task Given_ValidPayload_When_PostConvert_Then_Returns200AndValue()
    {
        var handler = new TestHttpMessageHandler();
        handler.EnqueueJson(new { @base = "AUD", date = "2025-09-10", rates = new { USD = 0.7m } });
        using var factory = new TestWebApplicationFactory(handler);
        var client = factory.CreateClient();

        var token = JwtTestHelper.CreateToken(claims: new[] { new Claim("scope", "exchange:read"), new Claim("scope", "exchange:write") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { amount = 10, inputCurrency = "AUD", outputCurrency = "USD" };
        var resp = await client.PostAsJsonAsync("/api/exchange/convert", payload);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<dynamic>();
        decimal value = (decimal)body!.value;
        value.Should().Be(7.00m);
    }

    [Fact]
    public async Task Given_MissingToken_When_PostConvert_Then_401()
    {
        var handler = new TestHttpMessageHandler();
        handler.EnqueueJson(new { @base = "AUD", date = "2025-09-10", rates = new { USD = 0.7m } });
        using var factory = new TestWebApplicationFactory(handler);
        var client = factory.CreateClient();

        var payload = new { amount = 10, inputCurrency = "AUD", outputCurrency = "USD" };
        var resp = await client.PostAsJsonAsync("/api/exchange/convert", payload);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Given_InsufficientScope_When_PostConvert_Then_403()
    {
        var handler = new TestHttpMessageHandler();
        handler.EnqueueJson(new { @base = "AUD", date = "2025-09-10", rates = new { USD = 0.7m } });
        using var factory = new TestWebApplicationFactory(handler);
        var client = factory.CreateClient();

        var token = JwtTestHelper.CreateToken(claims: new[] { new Claim("scope", "exchange:read") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { amount = 10, inputCurrency = "AUD", outputCurrency = "USD" };
        var resp = await client.PostAsJsonAsync("/api/exchange/convert", payload);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Given_RepeatedCalls_When_PostConvert_Then_UpstreamCalledOnceDueToCaching()
    {
        int callCount = 0;
        var handler = new TestHttpMessageHandler();
        handler.EnqueueResponse((req, ct) =>
        {
            Interlocked.Increment(ref callCount);
            var payload = new { @base = "AUD", date = "2025-09-10", rates = new { USD = 0.7m } };
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        });
        // For a second call, return same
        handler.EnqueueResponse((req, ct) =>
        {
            Interlocked.Increment(ref callCount);
            var payload = new { @base = "AUD", date = "2025-09-10", rates = new { USD = 0.7m } };
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        });

        using var factory = new TestWebApplicationFactory(handler);
        var client = factory.CreateClient();
        var token = JwtTestHelper.CreateToken(claims: new[] { new Claim("scope", "exchange:read"), new Claim("scope", "exchange:write") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payloadBody = new { amount = 10, inputCurrency = "AUD", outputCurrency = "USD" };
        var r1 = await client.PostAsJsonAsync("/api/exchange/convert", payloadBody);
        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        var r2 = await client.PostAsJsonAsync("/api/exchange/convert", payloadBody);
        r2.StatusCode.Should().Be(HttpStatusCode.OK);

        callCount.Should().Be(1, "hybrid cache should prevent a second upstream call for the same base currency");
    }

    [Fact]
    public async Task Given_Transient5xx_When_PostConvert_Then_RetryAndSucceed()
    {
        int callCount = 0;
        var handler = new TestHttpMessageHandler();
        handler.EnqueueResponse((req, ct) => { Interlocked.Increment(ref callCount); return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)); });
        handler.EnqueueJson(new { @base = "AUD", date = "2025-09-10", rates = new { USD = 0.7m } });

        using var factory = new TestWebApplicationFactory(handler);
        var client = factory.CreateClient();
        var token = JwtTestHelper.CreateToken(claims: new[] { new Claim("scope", "exchange:read"), new Claim("scope", "exchange:write") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.PostAsJsonAsync("/api/exchange/convert", new { amount = 10, inputCurrency = "AUD", outputCurrency = "USD" });
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        callCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Given_LowLimit_When_ExceedRateLimit_Then_429()
    {
        var config = new Dictionary<string, string?>
        {
            ["RateLimiting:ExchangeApiPermitLimit"] = "1",
            ["RateLimiting:ExchangeApiWindowMinutes"] = "10"
        };
        var handler = new TestHttpMessageHandler();
        handler.EnqueueJson(new { @base = "AUD", date = "2025-09-10", rates = new { USD = 0.7m } });
        handler.EnqueueJson(new { @base = "AUD", date = "2025-09-10", rates = new { USD = 0.7m } });
        using var factory = new TestWebApplicationFactory(handler, config);
        var client = factory.CreateClient();
        var token = JwtTestHelper.CreateToken(claims: new[] { new Claim("scope", "exchange:read"), new Claim("scope", "exchange:write") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = new { amount = 10, inputCurrency = "AUD", outputCurrency = "USD" };
        var ok = await client.PostAsJsonAsync("/api/exchange/convert", body);
        ok.StatusCode.Should().Be(HttpStatusCode.OK);
        var limited = await client.PostAsJsonAsync("/api/exchange/convert", body);
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Health_Endpoints_Work()
    {
        var handler = new TestHttpMessageHandler();
        using var factory = new TestWebApplicationFactory(handler);
        var client = factory.CreateClient();

        var live = await client.GetAsync("/health/live");
        live.StatusCode.Should().Be(HttpStatusCode.OK);

        var health = await client.GetAsync("/health");
        health.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
