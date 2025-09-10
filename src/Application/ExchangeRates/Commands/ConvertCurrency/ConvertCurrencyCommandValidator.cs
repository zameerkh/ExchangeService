using FluentValidation;

namespace ExchangeService.Application.ExchangeRates.Commands.ConvertCurrency;

/// <summary>
/// Validator for currency conversion command
/// </summary>
public class ConvertCurrencyCommandValidator : AbstractValidator<ConvertCurrencyCommand>
{
    public ConvertCurrencyCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero")
            .LessThanOrEqualTo(1_000_000_000)
            .WithMessage("Amount cannot exceed 1 billion");

        RuleFor(x => x.InputCurrency)
            .NotEmpty()
            .WithMessage("Input currency is required")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters")
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency code must contain only letters");

        RuleFor(x => x.OutputCurrency)
            .NotEmpty()
            .WithMessage("Output currency is required")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters")
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency code must contain only letters");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.InputCurrency, x.OutputCurrency, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Input and output currencies must be different")
            .OverridePropertyName(nameof(ConvertCurrencyCommand.OutputCurrency));
    }
}
