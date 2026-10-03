using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using PikminDetector.Api.Lib.CustomException;
using PikminDetector.Api.Lib.Host;

namespace PikminDetector.Api.Tests;

public sealed class ExceptionMiddlewareTests
{
    [Test]
    public async Task Invoke_CommonException_ReturnsProblemStatusDetailAndTraceId()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new CommonException(422, "Invalid image", "Use a portrait image."));

        await middleware.Invoke(context);

        using var problem = ReadProblem(context);
        Assert.That(context.Response.StatusCode, Is.EqualTo(422));
        Assert.That(context.Response.ContentType, Does.StartWith("application/problem+json"));
        Assert.That(problem.RootElement.GetProperty("title").GetString(), Is.EqualTo("Invalid image"));
        Assert.That(problem.RootElement.GetProperty("detail").GetString(), Is.EqualTo("Use a portrait image."));
        Assert.That(problem.RootElement.GetProperty("traceId").GetString(), Is.EqualTo(context.TraceIdentifier));
    }

    [Test]
    public async Task Invoke_UnexpectedException_ReturnsGenericProblemWithoutInternalDetails()
    {
        var context = CreateContext();
        var middleware = CreateMiddleware(_ => throw new InvalidOperationException("internal-secret"));

        await middleware.Invoke(context);

        using var problem = ReadProblem(context);
        Assert.That(context.Response.StatusCode, Is.EqualTo(500));
        Assert.That(problem.RootElement.GetRawText(), Does.Not.Contain("internal-secret"));
        Assert.That(problem.RootElement.GetProperty("detail").GetString(), Is.Not.Empty);
    }

    [Test]
    public async Task Invoke_ClientCancellation_DoesNotWriteAnErrorResponse()
    {
        var context = CreateContext();
        context.RequestAborted = new CancellationToken(true);
        var middleware = CreateMiddleware(_ => throw new OperationCanceledException());

        await middleware.Invoke(context);

        Assert.That(context.Response.Body.Length, Is.Zero);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/recognitions";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static ExceptionMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, NullLogger<ExceptionMiddleware>.Instance);

    private static JsonDocument ReadProblem(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(context.Response.Body);
    }
}
