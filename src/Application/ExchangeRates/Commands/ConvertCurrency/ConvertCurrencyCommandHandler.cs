using ExchangeService.Application.Common.Interfaces;
using ExchangeService.Domain.ValueObjects;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ExchangeService.Application.ExchangeRates.Commands.ConvertCurrency;

/// <summary>
/// Handler for currency conversion command
/// </summary>
public class ConvertCurrencyCommandHandler : IRequestHandler<ConvertCurrencyCommand, ConvertCurrencyResult>
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<ConvertCurrencyCommandHandler> _logger;

    public ConvertCurrencyCommandHandler(
        IExchangeRateService exchangeRateService,
        ILogger<ConvertCurrencyCommandHandler> logger)
    {
        _exchangeRateService = exchangeRateService;
        _logger = logger;
    }

    public async Task<ConvertCurrencyResult> Handle(ConvertCurrencyCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Converting {Amount} {InputCurrency} to {OutputCurrency}", 
            request.Amount, request.InputCurrency, request.OutputCurrency);

        // Create currency objects
        var inputCurrency = Currency.Create(request.InputCurrency, request.InputCurrency);
        var outputCurrency = Currency.Create(request.OutputCurrency, request.OutputCurrency);

        // Create original money amount
        var originalAmount = new Money(request.Amount, inputCurrency);

        // Get exchange rate
        var exchangeRate = await _exchangeRateService.GetExchangeRateAsync(inputCurrency, outputCurrency, cancellationToken);
        
        if (exchangeRate == null)
        {
            _logger.LogWarning("Exchange rate not found for {InputCurrency} to {OutputCurrency}", 
                request.InputCurrency, request.OutputCurrency);
            throw new InvalidOperationException($"Exchange rate not available for {request.InputCurrency} to {request.OutputCurrency}");
        }

        // Convert the amount
        var convertedAmount = exchangeRate.Convert(originalAmount);

        _logger.LogInformation("Successfully converted {OriginalAmount} to {ConvertedAmount} using rate {Rate}", 
            originalAmount, convertedAmount, exchangeRate.Rate);

        return new ConvertCurrencyResult
        {
            OriginalAmount = originalAmount,
            ConvertedAmount = convertedAmount,
            ExchangeRate = exchangeRate.Rate,
            Timestamp = exchangeRate.Timestamp
        };
    }
}
