using PikminDetector.Api.ErrorHandling;
using PikminDetector.Api.Repositories;
using PikminDetector.Api.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger(), dispose: true);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    if (allowedOrigins.Length > 0)
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
app.UseMiddleware<ExceptionMiddleware>();
if (allowedOrigins.Length > 0)
    app.UseCors();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
