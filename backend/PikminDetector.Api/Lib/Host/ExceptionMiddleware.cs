using Microsoft.AspNetCore.Mvc;
using PikminDetector.Api.Lib.CustomException;

namespace PikminDetector.Api.Lib.Host;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The caller disconnected; there is no response to send.
        }
        catch (Exception error) when (!context.Response.HasStarted)
        {
            var problem = error is CommonException known
                ? new ProblemDetails
                {
                    Status = known.StatusCode,
                    Title = known.Message,
                    Detail = known.Detail?.ToString() ?? known.Message
                }
                : new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred.",
                    Detail = "The request could not be completed. Please try again later."
                };

            if (problem.Status >= 500)
                logger.LogError(error, "Request failed: {Method} {Path}", context.Request.Method, context.Request.Path);
            problem.Instance = $"{context.Request.Method} {context.Request.Path}";
            problem.Extensions["traceId"] = context.TraceIdentifier;
            context.Response.StatusCode = problem.Status!.Value;
            await context.Response.WriteAsJsonAsync(problem, options: null,
                contentType: "application/problem+json", cancellationToken: context.RequestAborted);
        }
    }
}
