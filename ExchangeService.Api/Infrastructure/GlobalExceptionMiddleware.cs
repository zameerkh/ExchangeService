using System.Net;
using System.Text.Json;

namespace ExchangeService.Api.Infrastructure;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var problemDetails = new ProblemDetails();

        switch (exception)
        {
            case InvalidOperationException ex when ex.Message.Contains("rate limiting") || ex.Message.Contains("Rate limit"):
                response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                problemDetails.Title = "Rate Limit Exceeded";
                problemDetails.Detail = "Too many requests. Please try again later.";
                problemDetails.Status = (int)HttpStatusCode.TooManyRequests;
                break;

            case InvalidOperationException ex when ex.Message.Contains("timeout") || ex.Message.Contains("timed out"):
                response.StatusCode = (int)HttpStatusCode.GatewayTimeout;
                problemDetails.Title = "Request Timeout";
                problemDetails.Detail = "The request timed out. Please try again later.";
                problemDetails.Status = (int)HttpStatusCode.GatewayTimeout;
                break;

            case InvalidOperationException ex when ex.Message.Contains("unavailable") || ex.Message.Contains("service"):
                response.StatusCode = (int)HttpStatusCode.BadGateway;
                problemDetails.Title = "Service Unavailable";
                problemDetails.Detail = "The exchange rate service is temporarily unavailable. Please try again later.";
                problemDetails.Status = (int)HttpStatusCode.BadGateway;
                break;

            case InvalidOperationException ex when ex.Message.Contains("not supported") || ex.Message.Contains("not available"):
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Unsupported Currency";
                problemDetails.Detail = ex.Message;
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                break;

            case HttpRequestException ex:
                response.StatusCode = (int)HttpStatusCode.BadGateway;
                problemDetails.Title = "External Service Error";
                problemDetails.Detail = "Unable to retrieve current exchange rates. Please try again later.";
                problemDetails.Status = (int)HttpStatusCode.BadGateway;
                break;

            case OperationCanceledException when context.RequestAborted.IsCancellationRequested:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                problemDetails.Title = "Request Cancelled";
                problemDetails.Detail = "The request was cancelled.";
                problemDetails.Status = (int)HttpStatusCode.RequestTimeout;
                break;

            case ArgumentException ex:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                problemDetails.Title = "Invalid Request";
                problemDetails.Detail = ex.Message;
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                problemDetails.Title = "Internal Server Error";
                problemDetails.Detail = "An unexpected error occurred. Please try again later.";
                problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                break;
        }

        problemDetails.Instance = context.Request.Path;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var result = JsonSerializer.Serialize(problemDetails, jsonOptions);
        await response.WriteAsync(result);
    }
}

/// <summary>
/// Problem details model for consistent error responses
/// </summary>
public class ProblemDetails
{
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public int Status { get; set; }
    public string Instance { get; set; } = string.Empty;
}