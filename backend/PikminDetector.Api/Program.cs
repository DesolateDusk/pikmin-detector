using Microsoft.Extensions.Configuration.Json;
using Npgsql;
using PikminDetector.Api.ErrorHandling;
using PikminDetector.Api.Repositories;
using PikminDetector.Api.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 部署只採用注入的設定，避免本機檔案內容影響正式設定。
if (!builder.Environment.IsDevelopment())
{
    var localSources = builder.Configuration.Sources.OfType<JsonConfigurationSource>()
        .Where(source => source.Path is "appsettings.json" ||
            source.Path == $"appsettings.{builder.Environment.EnvironmentName}.json").ToArray();
    foreach (var source in localSources)
        builder.Configuration.Sources.Remove(source);
}

// Render terminates TLS and forwards HTTP to the container's assigned port.
if (builder.Configuration["PORT"] is { Length: > 0 } port)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();
builder.Logging.AddSerilog(new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger(), dispose: true);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
    options.AddDefaultPolicy(policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddScoped<ISpotService, SpotService>();
builder.Services.AddScoped<ISpotRepository, SpotRepository>();
builder.Services.AddScoped<IRecognitionService, RecognitionService>();
builder.Services.AddScoped<IRecognitionCatalogRepository, RecognitionCatalogRepository>();
builder.Services.AddHttpClient<TreelazySource>(client => client.Timeout = TimeSpan.FromSeconds(90));
builder.Services.AddScoped<SpotImportRepository>();
builder.Services.AddScoped<SpotImportService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    var connectionString = app.Configuration.GetConnectionString("Pikmin");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("請透過 Doppler/部署平台設定 CONNECTIONSTRINGS__PIKMIN。");
    try
    {
        var database = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(database.Host) || string.IsNullOrWhiteSpace(database.Database) ||
            string.IsNullOrWhiteSpace(database.Username))
            throw new ArgumentException();
    }
    catch (ArgumentException)
    {
        // 不附帶原始例外，避免連線字串中的秘密出現在啟動 log。
        throw new InvalidOperationException("CONNECTIONSTRINGS__PIKMIN 必須是包含 Host、Database、Username 的 Npgsql 連線字串。");
    }

    var origins = app.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length == 0 || origins.Any(origin =>
        !Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
        uri.UserInfo.Length > 0 || origin != uri.GetLeftPart(UriPartial.Authority)))
        throw new InvalidOperationException("請設定 ALLOWEDORIGINS__0 等 HTTPS origin，不可包含路徑或結尾斜線。");
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
