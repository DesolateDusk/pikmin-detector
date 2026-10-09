using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using PikminDetector.Api.Models;
using PikminDetector.Api.Repositories;
using PikminDetector.Api.Services;
using Serilog;

namespace PikminDetector.Api.Lib.Host;

public static class HostBuilderWeb
{
    public static void SetupConfiguration(WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<Database>().BindConfiguration("ConnectionStrings");
        builder.Services.AddOptions<AppSettings>().BindConfiguration("");

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
}
