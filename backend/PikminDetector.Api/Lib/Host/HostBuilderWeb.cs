using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Npgsql;
using PikminDetector.Api.Models;
using PikminDetector.Api.Repositories;
using PikminDetector.Api.Services;
using Serilog;

namespace PikminDetector.Api.Lib.Host;

public static class HostBuilderWeb
{
    public static void SetupConfiguration(WebApplicationBuilder builder)
    {
        var production = !builder.Environment.IsDevelopment();
        builder.Services.AddOptions<Database>()
            .BindConfiguration("ConnectionStrings")
            .Validate(settings => !production || IsValidConnectionString(settings.Pikmin),
                "ConnectionStrings:Pikmin must be a valid Npgsql connection string.")
            .ValidateOnStart();
        builder.Services.AddOptions<AppSettings>()
            .BindConfiguration("")
            .Validate(settings => (!production || settings.AllowedOrigins.Length > 0) &&
                settings.AllowedOrigins.All(origin => IsValidOrigin(origin, production)),
                "AllowedOrigins must contain valid site origins without paths.")
            .ValidateOnStart();

        builder.Services.AddCors();
        builder.Services.AddOptions<CorsOptions>().Configure<IOptions<AppSettings>>((options, settings) =>
            options.AddDefaultPolicy(policy => policy.WithOrigins(settings.Value.AllowedOrigins)
                .AllowAnyMethod().AllowAnyHeader()));

        builder.Services.AddSerilog((services, logger) => logger
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(builder.Environment.ContentRootPath, "logs", "api-.log"),
                rollingInterval: RollingInterval.Day, shared: true));

        if (builder.Configuration.GetValue<int?>("PORT") is { } port)
        {
            if (port is < 1 or > 65535)
                throw new InvalidOperationException("PORT must be between 1 and 65535.");
            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
        }
    }

    public static void AddApplicationServices(IServiceCollection services)
    {
        services.AddSingleton<IConnectionProviderFactory, ConnectionProviderFactory>();
        services.AddScoped<IDbContext, DbContext>();
        services.AddScoped<ISpotService, SpotService>();
        services.AddScoped<IRecognitionService, RecognitionService>();
        services.AddScoped<ISpotImportService, SpotImportService>();
        services.AddScoped<ISpotRepository, SpotRepository>();
        services.AddScoped<IRecognitionCatalogRepository, RecognitionCatalogRepository>();
        services.AddScoped<ISpotImportRepository, SpotImportRepository>();
        services.AddHttpClient<ITreelazySource, TreelazySource>(client =>
            client.Timeout = TimeSpan.FromSeconds(90));
    }

    private static bool IsValidOrigin(string origin, bool production) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        (uri.Scheme == "https" || (!production && uri.Scheme == "http")) &&
        string.IsNullOrEmpty(uri.UserInfo) &&
        uri.GetLeftPart(UriPartial.Authority) == origin;

    private static bool IsValidConnectionString(string value)
    {
        try
        {
            var connection = new NpgsqlConnectionStringBuilder(value);
            return !string.IsNullOrWhiteSpace(connection.Host) &&
                   !string.IsNullOrWhiteSpace(connection.Database) &&
                   !string.IsNullOrWhiteSpace(connection.Username);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
