using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Npgsql;
using PikminDetector.Api.Common.Errors;

namespace PikminDetector.Api.ErrorHandling;

public sealed class ExceptionMiddleware
{
    private const string GenericServerError = "The request could not be completed.";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate next;
    private readonly ILogger<ExceptionMiddleware> logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await WriteErrorAsync(context, exception);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var (statusCode, detail) = GetError(exception);
        var title = ReasonPhrases.GetReasonPhrase(statusCode);
        if (string.IsNullOrWhiteSpace(title))
        {
            title = "Request Failed";
        }

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Request failed with status {StatusCode}", statusCode);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        var problem = ApiErrorResponseFactory.Create(context, statusCode, title, detail);
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            problem,
            JsonOptions);
    }

    private static (int StatusCode, string Detail) GetError(Exception exception)
    {
        if (exception is AppException appException)
        {
            return (appException.StatusCode, appException.Message);
        }

        if (exception is NpgsqlException { InnerException: TimeoutException })
        {
            return (StatusCodes.Status504GatewayTimeout, "Spot data service timed out.");
        }

        if (exception is NpgsqlException)
        {
            return (StatusCodes.Status503ServiceUnavailable, "Spot data service is unavailable.");
        }

        if (exception is TimeoutException)
        {
            return (StatusCodes.Status504GatewayTimeout, "Spot data service timed out.");
        }

        if (exception is HttpRequestException or InvalidDataException or System.Text.Json.JsonException)
        {
            return (StatusCodes.Status502BadGateway, "Spot source returned an unusable response.");
        }

        return (StatusCodes.Status500InternalServerError, GenericServerError);
    }
}
