namespace PikminDetector.Api.Common.Errors;

// Application failures carry their HTTP status directly to avoid a second error-code mapping.
public sealed class AppException : Exception
{
    public AppException(int statusCode, string message)
        : base(message)
    {
        if (statusCode is < 400 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCode));
        }

        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
