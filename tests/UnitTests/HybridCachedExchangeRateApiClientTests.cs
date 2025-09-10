using ExchangeService.Api.Configuration;
using ExchangeService.Api.Models;
using ExchangeService.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace ExchangeService.Tests.UnitTests;

[Trait("Category", "Unit")]
public class HybridCachedExchangeRateApiClientTests
{
    [Fact]
    public async Task Given_CacheMiss_When_GetExchangeRatesAsync_Then_CallsInnerAndPopulatesCache()
    {
        var inner = Substitute.For<IExchangeRateApiClient>();
        inner.GetExchangeRatesAsync("AUD", Arg.Any<CancellationToken>())
            .Returns(new ExchangeRateApiResponse { Base = "AUD", Rates = new() { ["USD"] = 0.7m } });

        using var hybrid = new HybridCache(new HybridCacheOptions());
        var logger = Substitute.For<ILogger<HybridCachedExchangeRateApiClient>>();
        var options = Options.Create(new CachingOptions { ExchangeRatesCacheMinutes = 5 });

        var sut = new HybridCachedExchangeRateApiClient(inner, hybrid, logger, options);

        var resp1 = await sut.GetExchangeRatesAsync("AUD");
        var resp2 = await sut.GetExchangeRatesAsync("AUD");

        resp1.Rates["USD"].Should().Be(0.7m);
        resp2.Rates["USD"].Should().Be(0.7m);
        await inner.Received(1).GetExchangeRatesAsync("AUD", Arg.Any<CancellationToken>());
    }
}
