namespace PikminDetector.Api.Models;

public sealed class AppSettings
{
    public string[] AllowedOrigins { get; set; } = [];
}

public sealed class Database
{
    public string Pikmin { get; set; } = string.Empty;
}
