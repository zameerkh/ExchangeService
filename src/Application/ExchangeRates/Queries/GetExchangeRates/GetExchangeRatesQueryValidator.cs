using FluentValidation;

namespace ExchangeService.Application.ExchangeRates.Queries.GetExchangeRates;

/// <summary>
/// Validator for getting exchange rates query
/// </summary>
public class GetExchangeRatesQueryValidator : AbstractValidator<GetExchangeRatesQuery>
{
    public GetExchangeRatesQueryValidator()
    {
        RuleFor(x => x.InputCurrency)
            .NotEmpty()
            .WithMessage("Input currency is required")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters")
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency code must contain only letters");

        RuleForEach(x => x.OutputCurrencies)
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters")
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency code must contain only letters");

        RuleFor(x => x.OutputCurrencies)
            .Must(currencies => currencies.Count() <= 50)
            .WithMessage("Cannot request more than 50 currencies at once");
    }
}
