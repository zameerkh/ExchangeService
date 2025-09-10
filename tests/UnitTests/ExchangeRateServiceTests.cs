using ExchangeService.Api.Models;
using ExchangeService.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ExchangeService.Tests.UnitTests;

[Trait("Category", "Unit")]
public class ExchangeRateServiceTests
{
    [Fact]
    public async Task Given_ValidRequest_When_ConvertCurrencyAsync_Then_UsesRateAndRounds()
    {
        var client = Substitute.For<IExchangeRateApiClient>();
        client.GetExchangeRatesAsync("AUD", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ExchangeRateApiResponse
            {
                Base = "AUD",
                Rates = new() { ["USD"] = 0.65m }
            }));
        var logger = Substitute.For<ILogger<ExchangeRateService>>();
        var sut = new ExchangeRateService(client, logger);

        var req = new ExchangeRequest { Amount = 10.123m, InputCurrency = "aud", OutputCurrency = "usd" };
        var resp = await sut.ConvertCurrencyAsync(req);

        resp.Value.Should().Be(Math.Round(10.123m * 0.65m, 2, MidpointRounding.AwayFromZero));
        resp.ExchangeRate.Should().Be(0.65m);
        resp.InputCurrency.Should().Be("AUD");
        resp.OutputCurrency.Should().Be("USD");
    }

    [Fact]
    public async Task Given_SameCurrency_When_ConvertCurrencyAsync_Then_ReturnsOriginalAmount()
    {
        var client = Substitute.For<IExchangeRateApiClient>();
        var logger = Substitute.For<ILogger<ExchangeRateService>>();
        var sut = new ExchangeRateService(client, logger);

        var req = new ExchangeRequest { Amount = 20m, InputCurrency = "USD", OutputCurrency = "USD" };
        var resp = await sut.ConvertCurrencyAsync(req);

        resp.Value.Should().Be(20m);
        resp.ExchangeRate.Should().Be(1.0m);
        resp.FromCache.Should().BeTrue();
    }

    [Fact]
    public async Task Given_MissingRate_When_ConvertCurrencyAsync_Then_ThrowsInvalidOperation()
    {
        var client = Substitute.For<IExchangeRateApiClient>();
        client.GetExchangeRatesAsync("AUD", Arg.Any<CancellationToken>())
            .Returns(new ExchangeRateApiResponse { Base = "AUD", Rates = new() });
        var logger = Substitute.For<ILogger<ExchangeRateService>>();
        var sut = new ExchangeRateService(client, logger);

        var req = new ExchangeRequest { Amount = 10, InputCurrency = "AUD", OutputCurrency = "USD" };

        var act = async () => await sut.ConvertCurrencyAsync(req);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
