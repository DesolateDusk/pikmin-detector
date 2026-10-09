namespace PikminDetector.Api.Lib.CustomException;

public class CommonException : Exception
{
    public int StatusCode { get; }
    public object? Detail { get; }

    public CommonException(int statusCode, string message, object? detail = null) : base(message)
    {
        if (statusCode is < 400 or > 599)
            throw new ArgumentOutOfRangeException(nameof(statusCode));
        StatusCode = statusCode;
        Detail = detail;
    }
}