using ExchangeService.Application.Common.Interfaces;
using ExchangeService.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ExchangeService.Application.ExchangeRates.Queries.GetExchangeRates;

/// <summary>
/// Handler for getting exchange rates query
/// </summary>
public class GetExchangeRatesQueryHandler : IRequestHandler<GetExchangeRatesQuery, GetExchangeRatesResult>
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<GetExchangeRatesQueryHandler> _logger;

    public GetExchangeRatesQueryHandler(
        IExchangeRateService exchangeRateService,
        ILogger<GetExchangeRatesQueryHandler> logger)
    {
        _exchangeRateService = exchangeRateService;
        _logger = logger;
    }

    public async Task<GetExchangeRatesResult> Handle(GetExchangeRatesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting exchange rates for {InputCurrency} to [{OutputCurrencies}]", 
            request.InputCurrency, string.Join(", ", request.OutputCurrencies));

        // Create currency objects
        var baseCurrency = Currency.Create(request.InputCurrency, request.InputCurrency);
        var targetCurrencies = request.OutputCurrencies
            .Select(code => Currency.Create(code, code))
            .ToList();

        // Get exchange rates
        IEnumerable<Domain.Entities.ExchangeRate> exchangeRates;
        
        if (targetCurrencies.Any())
        {
            exchangeRates = await _exchangeRateService.GetExchangeRatesAsync(baseCurrency, targetCurrencies, cancellationToken);
        }
        else
        {
            exchangeRates = await _exchangeRateService.GetAllExchangeRatesAsync(baseCurrency, cancellationToken);
        }

        var exchangeRatesList = exchangeRates.ToList();
        
        _logger.LogInformation("Retrieved {Count} exchange rates for {InputCurrency}", 
            exchangeRatesList.Count, request.InputCurrency);

        // Map to DTOs
        var exchangeRateDtos = exchangeRatesList.Select(er => new ExchangeRateDto
        {
            Currency = er.TargetCurrency.Code,
            Rate = er.Rate,
            Timestamp = er.Timestamp
        }).ToList();

        return new GetExchangeRatesResult
        {
            BaseCurrency = request.InputCurrency,
            ExchangeRates = exchangeRateDtos,
            Timestamp = exchangeRatesList.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow
        };
    }
}
