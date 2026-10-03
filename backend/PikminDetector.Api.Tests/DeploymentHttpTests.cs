using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PikminDetector.Api.Services;
using PikminDetector.Api.Controllers;

namespace PikminDetector.Api.Tests;

public sealed class DeploymentHttpTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Configuration_JsonOrEnvironment_BindsTheSameDatabaseAndCorsPolicy(bool useEnvironment)
    {
        const string origin = "https://configured.example";
        const string connection = "Host=db.example;Database=pikmin;Username=test";
        var prefix = $"PIKMIN_TEST_{Guid.NewGuid():N}_";
        try
        {
            Environment.SetEnvironmentVariable(prefix + "CONNECTIONSTRINGS__PIKMIN", connection);
            Environment.SetEnvironmentVariable(prefix + "ALLOWEDORIGINS__0", origin);
            using var factory = new WebApplicationFactory<SpotsController>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.Sources.Clear();
                    if (useEnvironment)
                        config.AddEnvironmentVariables(prefix);
                    else
                        config.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(
                            """
                            {"ConnectionStrings":{"Pikmin":"Host=db.example;Database=pikmin;Username=test"},
                             "AllowedOrigins":["https://configured.example"]}
                            """)));
                });
            });
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Options, "/api/recognitions");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");

            var response = await client.SendAsync(request);

            Assert.That(response.Headers.GetValues("Access-Control-Allow-Origin").Single(), Is.EqualTo(origin));
            Assert.That(factory.Services.GetRequiredService<IOptions<PikminDetector.Api.Models.Database>>().Value.Pikmin,
                Is.EqualTo(connection));
        }
        finally
        {
            Environment.SetEnvironmentVariable(prefix + "CONNECTIONSTRINGS__PIKMIN", null);
            Environment.SetEnvironmentVariable(prefix + "ALLOWEDORIGINS__0", null);
        }
    }

    [Test]
    public void Startup_ResolvesImportServiceAndTypedHttpClient()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();

        Assert.That(scope.ServiceProvider.GetRequiredService<ISpotImportService>(), Is.TypeOf<SpotImportService>());
        Assert.That(scope.ServiceProvider.GetRequiredService<ITreelazySource>(), Is.TypeOf<TreelazySource>());
    }

    [TestCase("Host=db.example;Database=pikmin;Username=test")]
    [TestCase("Host=db.example;Database=pikmin;Username=test;Password=abc${xyz}")]
    public async Task Health_ProductionHttp_ReturnsOkWithoutRedirectOrDatabase(string connectionString)
    {
        using var factory = CreateFactory(connectionString);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("{\"status\":\"ok\"}"));
        Assert.That(response.Headers.Location, Is.Null);
    }
    [TestCase("https://desolatedusk.github.io", true)]
    [TestCase("https://untrusted.example", false)]
    public async Task Preflight_ProductionOrigin_AllowsOnlyConfiguredSite(string origin, bool allowed)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/recognitions");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        if (allowed)
            Assert.That(response.Headers.GetValues("Access-Control-Allow-Origin").Single(), Is.EqualTo(origin));
        else
            Assert.That(response.Headers.Contains("Access-Control-Allow-Origin"), Is.False);
    }
    [TestCase("", "https://desolatedusk.github.io", "ConnectionStrings:Pikmin")]
    [TestCase("${shared.prd.DB}", "https://desolatedusk.github.io", "ConnectionStrings:Pikmin")]
    [TestCase("postgresql://user:secret@db.example/pikmin", "https://desolatedusk.github.io", "Npgsql")]
    [TestCase("Host=db.example;Database=pikmin;Username=test", "", "AllowedOrigins")]
    [TestCase("Host=db.example;Database=pikmin;Username=test", "https://desolatedusk.github.io/pikmin-detector/", "AllowedOrigins")]
    [TestCase("Host=db.example;Database=pikmin;Username=test", "https://user:password@configured.example", "AllowedOrigins")]
    public void Startup_ProductionInvalidConfiguration_StopsWithSettingName(
        string connectionString, string origin, string expectedSetting)
    {
        using var factory = CreateFactory(connectionString, origin);

        var error = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.That(error!.Message, Does.Contain(expectedSetting));
        Assert.That(error.ToString(), Does.Not.Contain("secret@"));
    }

    private static WebApplicationFactory<SpotsController> CreateFactory(
        string connectionString = "Host=db.example;Database=pikmin;Username=test",
        string origin = "https://desolatedusk.github.io") =>
        new WebApplicationFactory<SpotsController>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Pikmin"] = connectionString,
                    ["AllowedOrigins:0"] = origin
                }));
        });
}
