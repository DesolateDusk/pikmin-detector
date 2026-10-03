using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using PikminDetector.Api.Controllers;

namespace PikminDetector.Api.Tests;

public sealed class DeploymentHttpTests
{
    [Theory]
    [InlineData("Host=db.example;Database=pikmin;Username=test")]
    [InlineData("Host=db.example;Database=pikmin;Username=test;Password=abc${xyz}")]
    public async Task Health_ProductionHttp_ReturnsOkWithoutRedirectOrDatabase(string connectionString)
    {
        using var factory = CreateFactory(connectionString);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"status\":\"ok\"}", await response.Content.ReadAsStringAsync());
        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData("https://desolatedusk.github.io", true)]
    [InlineData("https://untrusted.example", false)]
    public async Task Preflight_ProductionOrigin_AllowsOnlyConfiguredSite(string origin, bool allowed)
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/recognitions");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        if (allowed)
            Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        else
            Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("", "https://desolatedusk.github.io", "CONNECTIONSTRINGS__PIKMIN")]
    [InlineData("${shared.prd.DB}", "https://desolatedusk.github.io", "CONNECTIONSTRINGS__PIKMIN")]
    [InlineData("postgresql://user:secret@db.example/pikmin", "https://desolatedusk.github.io", "Npgsql")]
    [InlineData("Host=db.example;Database=pikmin;Username=test", "", "ALLOWEDORIGINS__0")]
    [InlineData("Host=db.example;Database=pikmin;Username=test", "https://desolatedusk.github.io/pikmin-detector/", "ALLOWEDORIGINS__0")]
    public void Startup_ProductionInvalidConfiguration_StopsWithSettingName(
        string connectionString, string origin, string expectedSetting)
    {
        using var factory = CreateFactory(connectionString, origin);

        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains(expectedSetting, error.Message);
        Assert.DoesNotContain("secret@", error.ToString());
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
