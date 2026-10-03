namespace PikminDetector.Api.Lib.Host;

public static class MiddlewareExtensions
{
    public static WebApplication UseMiddlewareExtensions(this WebApplication app)
    {
        app.UseMiddleware<ExceptionMiddleware>();
        return app;
    }
}
