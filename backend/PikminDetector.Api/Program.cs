using PikminDetector.Api.Lib.Host;

var builder = WebApplication.CreateBuilder(args);

HostBuilderWeb.SetupConfiguration(builder);

HostBuilderWeb.AddApplicationServices(builder.Services);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddlewareExtensions();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
