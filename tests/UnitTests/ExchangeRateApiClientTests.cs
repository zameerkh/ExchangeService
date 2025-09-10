using System.Net;
using System.Net.Http;
using ExchangeService.Api.Configuration;
using ExchangeService.Api.Services;
using ExchangeService.Tests.TestUtilities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace ExchangeService.Tests.UnitTests;

[Trait("Category", "Unit")]
public class ExchangeRateApiClientTests
{
    [Fact]
    public async Task Given_SuccessfulResponse_When_GetExchangeRatesAsync_Then_ParsesJson()
    {
        var handler = new TestHttpMessageHandler();
        handler.EnqueueJson(new { @base = "AUD", date = "2025-09-10", rates = new { USD = 0.66m } });
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.example/") };
        var options = Options.Create(new ExchangeRateApiOptions { BaseUrl = "https://api.example/" });
        var logger = Substitute.For<ILogger<ExchangeRateApiClient>>();

        var sut = new ExchangeRateApiClient(http, options, logger);
        var resp = await sut.GetExchangeRatesAsync("aud");

        resp.Base.Should().Be("AUD");
        resp.Rates["USD"].Should().Be(0.66m);
    }

    [Fact]
    public async Task Given_TooManyRequests_When_GetExchangeRatesAsync_Then_ThrowsHttpRequestException()
    {
        var handler = new TestHttpMessageHandler();
        handler.EnqueueStatus(HttpStatusCode.TooManyRequests);
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.example/") };
        var options = Options.Create(new ExchangeRateApiOptions { BaseUrl = "https://api.example/" });
        var logger = Substitute.For<ILogger<ExchangeRateApiClient>>();

        var sut = new ExchangeRateApiClient(http, options, logger);
        var act = async () => await sut.GetExchangeRatesAsync("AUD");
        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
