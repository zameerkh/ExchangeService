using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using ExchangeService.Api.Models;
using ExchangeService.Api.Services;
using System.Net;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OutputCaching;
using ExchangeService.Api.Infrastructure;
using System.Diagnostics;

namespace ExchangeService.Api.Controllers;

/// <summary>
/// Controller for currency exchange operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExchangeController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly IValidator<ExchangeRequest> _validator;
    private readonly ILogger<ExchangeController> _logger;

    /// <summary>
    /// Initializes a new instance of the ExchangeController
    /// </summary>
    /// <param name="exchangeRateService">Exchange rate service</param>
    /// <param name="validator">Request validator</param>
    /// <param name="logger">Logger instance</param>
    public ExchangeController(
        IExchangeRateService exchangeRateService,
        IValidator<ExchangeRequest> validator,
        ILogger<ExchangeController> logger)
    {
        _exchangeRateService = exchangeRateService ?? throw new ArgumentNullException(nameof(exchangeRateService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Converts currency from one type to another
    /// </summary>
    /// <param name="request">The exchange request containing amount and currency details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The converted amount in the target currency</returns>
    /// <response code="200">Currency conversion successful</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized - valid JWT token required</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    /// <response code="429">Rate limit exceeded</response>
    /// <response code="502">External service unavailable</response>
    /// <response code="504">Request timeout</response>
    [HttpPost("convert")]
    [Authorize(Policy = "ExchangeService")]
    [EnableRateLimiting("ExchangeApi")]
    [OutputCache(PolicyName = "ExchangeRates")]
    [ProducesResponseType(typeof(ExchangeResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), (int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), (int)HttpStatusCode.TooManyRequests)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), (int)HttpStatusCode.BadGateway)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), (int)HttpStatusCode.GatewayTimeout)]
    public async Task<ActionResult<ExchangeResponse>> ConvertCurrency(
        [FromBody] ExchangeRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = TelemetryExtensions.StartCurrencyConversionActivity(
            "exchange.convert",
            request?.InputCurrency ?? "unknown",
            request?.OutputCurrency ?? "unknown",
            request?.Amount ?? 0);

        // Add correlation ID to logs
        var correlationId = HttpContext.TraceIdentifier;
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = User.Identity?.Name ?? "anonymous"
        });

        if (request == null)
        {
            _logger.LogWarning("Received null exchange request");
            activity?.RecordError(new ArgumentNullException(nameof(request), "Request body cannot be null"));
            
            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Request body cannot be null",
                Status = (int)HttpStatusCode.BadRequest
            });
        }

        // Validate the request using FluentValidation
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for exchange request: {ValidationErrors}", 
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));

            var validationException = new ValidationException("Request validation failed");
            activity?.RecordError(validationException);

            var problemDetails = new ValidationProblemDetails();
            foreach (var error in validationResult.Errors)
            {
                if (!problemDetails.Errors.ContainsKey(error.PropertyName))
                {
                    problemDetails.Errors[error.PropertyName] = new string[] { };
                }
                problemDetails.Errors[error.PropertyName] = 
                    problemDetails.Errors[error.PropertyName].Append(error.ErrorMessage).ToArray();
            }

            problemDetails.Title = "Validation Failed";
            problemDetails.Status = (int)HttpStatusCode.BadRequest;

            return BadRequest(problemDetails);
        }

        _logger.LogInformation("Processing exchange request: {Amount} {InputCurrency} to {OutputCurrency}", 
            request.Amount, request.InputCurrency, request.OutputCurrency);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Let the global exception middleware handle any exceptions
            var response = await _exchangeRateService.ConvertCurrencyAsync(request, cancellationToken);

            stopwatch.Stop();
            
            // Record business metrics in telemetry
            activity?.RecordBusinessMetrics(
                response.ExchangeRate, 
                stopwatch.Elapsed.TotalMilliseconds,
                response.FromCache ? "cache" : "api");

            _logger.LogInformation("Exchange request completed successfully: {Amount} {InputCurrency} = {Value} {OutputCurrency} (took {ElapsedMs}ms)", 
                response.Amount, response.InputCurrency, response.Value, response.OutputCurrency, stopwatch.ElapsedMilliseconds);

            return Ok(response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.RecordError(ex);
            throw; // Let global exception middleware handle it
        }
    }

    /// <summary>
    /// Gets current exchange rates for a currency pair
    /// </summary>
    /// <param name="inputCurrency">Source currency code (e.g., "USD")</param>
    /// <param name="outputCurrency">Target currency code (e.g., "EUR")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current exchange rate information</returns>
    /// <response code="200">Exchange rate retrieved successfully</response>
    /// <response code="400">Invalid currency codes</response>
    /// <response code="401">Unauthorized - valid JWT token required</response>
    /// <response code="403">Forbidden - insufficient permissions</response>
    /// <response code="429">Rate limit exceeded</response>
    /// <response code="502">External service unavailable</response>
    /// <response code="504">Request timeout</response>
    [HttpGet("rates")]
    [Authorize(Policy = "ReadOnly")]
    [EnableRateLimiting("ExchangeApi")]
    [OutputCache(PolicyName = "ExchangeRates")]
    [ProducesResponseType(typeof(ExchangeRateInfo), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), (int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), (int)HttpStatusCode.TooManyRequests)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), (int)HttpStatusCode.BadGateway)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), (int)HttpStatusCode.GatewayTimeout)]
    public async Task<ActionResult<ExchangeRateInfo>> GetExchangeRates(
        [FromQuery] string inputCurrency,
        [FromQuery] string outputCurrency,
        CancellationToken cancellationToken = default)
    {
        using var activity = TelemetryExtensions.StartCurrencyConversionActivity(
            "exchange.get_rates",
            inputCurrency ?? "unknown",
            outputCurrency ?? "unknown",
            0);

        // Add correlation ID to logs
        var correlationId = HttpContext.TraceIdentifier;
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = User.Identity?.Name ?? "anonymous"
        });

        if (string.IsNullOrEmpty(inputCurrency) || string.IsNullOrEmpty(outputCurrency))
        {
            _logger.LogWarning("Invalid currency parameters: InputCurrency={InputCurrency}, OutputCurrency={OutputCurrency}",
                inputCurrency, outputCurrency);
            
            var validationException = new ArgumentException("Currency codes cannot be null or empty");
            activity?.RecordError(validationException);

            return BadRequest(new ValidationProblemDetails
            {
                Title = "Invalid Parameters",
                Detail = "Both inputCurrency and outputCurrency are required",
                Status = (int)HttpStatusCode.BadRequest
            });
        }

        _logger.LogInformation("Getting exchange rates: {InputCurrency} to {OutputCurrency}", 
            inputCurrency, outputCurrency);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Create a minimal request to get exchange rate
            var exchangeRequest = new ExchangeRequest 
            { 
                Amount = 1, 
                InputCurrency = inputCurrency, 
                OutputCurrency = outputCurrency 
            };

            var response = await _exchangeRateService.ConvertCurrencyAsync(exchangeRequest, cancellationToken);

            stopwatch.Stop();
            
            // Record business metrics in telemetry
            activity?.RecordBusinessMetrics(
                response.ExchangeRate, 
                stopwatch.Elapsed.TotalMilliseconds,
                response.FromCache ? "cache" : "api");

            var rateInfo = new ExchangeRateInfo
            {
                InputCurrency = response.InputCurrency,
                OutputCurrency = response.OutputCurrency,
                ExchangeRate = response.ExchangeRate,
                Timestamp = response.Timestamp,
                FromCache = response.FromCache,
                RequestId = response.RequestId
            };

            _logger.LogInformation("Exchange rates retrieved successfully: 1 {InputCurrency} = {ExchangeRate} {OutputCurrency} (took {ElapsedMs}ms)", 
                response.InputCurrency, response.ExchangeRate, response.OutputCurrency, stopwatch.ElapsedMilliseconds);

            return Ok(rateInfo);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.RecordError(ex);
            throw; // Let global exception middleware handle it
        }
    }
}