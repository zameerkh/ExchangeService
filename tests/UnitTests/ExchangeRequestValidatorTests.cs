using ExchangeService.Api.Models;
using FluentAssertions;
using Xunit;

namespace ExchangeService.Tests.UnitTests;

[Trait("Category", "Unit")]
public class ExchangeRequestValidatorTests
{
    [Fact]
    public async Task Given_AmountLessOrEqualZero_When_Validated_Then_FailsWith400Message()
    {
        var validator = new ExchangeRequestValidator();
        var req = new ExchangeRequest { Amount = 0, InputCurrency = "AUD", OutputCurrency = "USD" };

        var result = await validator.ValidateAsync(req);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(ExchangeRequest.Amount));
    }

    [Fact]
    public async Task Given_SameCurrencies_When_Validated_Then_Fails()
    {
        var validator = new ExchangeRequestValidator();
        var req = new ExchangeRequest { Amount = 10, InputCurrency = "AUD", OutputCurrency = "AUD" };

        var result = await validator.ValidateAsync(req);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Given_InvalidCurrencyCode_When_Validated_Then_Fails()
    {
        var validator = new ExchangeRequestValidator();
        var req = new ExchangeRequest { Amount = 10, InputCurrency = "AUD", OutputCurrency = "ZZZ" };

        var result = await validator.ValidateAsync(req);

        result.IsValid.Should().BeFalse();
    }
}
