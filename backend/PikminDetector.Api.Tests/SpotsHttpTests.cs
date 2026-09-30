using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PikminDetector.Api.Controllers;
using PikminDetector.Api.Models;
using PikminDetector.Api.Repositories;

namespace PikminDetector.Api.Tests;

public sealed class SpotsHttpTests
{
    [Fact]
    public async Task SearchArea_WithCountryCityAreaAndDecor_ForwardsFiltersAndReturnsDecorTypes()
    {
        var repository = new RecordingRepository();
        using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();

        var spots = await client.GetFromJsonAsync<SpotVM[]>(
            "/api/spots?country=TW&city=%E8%87%BA%E5%8C%97%E5%B8%82&area=%E4%BF%A1%E7%BE%A9%E5%8D%80&decorTypeKey=park");

        Assert.Equal(new AreaSpotFilter("TW", "臺北市", "信義區", "park", 50), repository.AreaFilter);
        var spot = Assert.Single(spots!);
        Assert.Equal("TW", spot.Country);
        Assert.Equal("park", Assert.Single(spot.DecorTypes).Key);
    }

    [Fact]
    public async Task SearchArea_WithoutCountry_ReturnsValidationProblem()
    {
        using var factory = CreateFactory(new RecordingRepository());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/spots?city=Taipei");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task SearchArea_AreaWithoutCity_ReturnsValidationProblem()
    {
        using var factory = CreateFactory(new RecordingRepository());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/spots?country=TW&area=%E4%BF%A1%E7%BE%A9%E5%8D%80");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FindNearby_WithRadiusAndDecor_ForwardsFiltersAndReturnsDistance()
    {
        var repository = new RecordingRepository();
        using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();

        var spots = await client.GetFromJsonAsync<SpotVM[]>(
            "/api/spots/nearby?latitude=25.03&longitude=121.52&radiusMeters=500&decorTypeKey=cafe");

        Assert.Equal(new NearbySpotFilter(25.03, 121.52, 500, "cafe", 50), repository.NearbyFilter);
        Assert.Equal(120, Assert.Single(spots!).DistanceMeters);
    }

    [Theory]
    [InlineData("/api/spots/nearby?latitude=91&longitude=121")]
    [InlineData("/api/spots/nearby?latitude=25&longitude=181")]
    [InlineData("/api/spots/nearby?latitude=25&longitude=121&radiusMeters=0")]
    [InlineData("/api/spots/nearby?latitude=25&longitude=121&limit=101")]
    public async Task FindNearby_InvalidNumericInput_ReturnsValidationProblem(string path)
    {
        using var factory = CreateFactory(new RecordingRepository());
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static WebApplicationFactory<SpotsController> CreateFactory(RecordingRepository repository) =>
        new WebApplicationFactory<SpotsController>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISpotRepository>();
                services.AddSingleton<ISpotRepository>(repository);
            }));

    private sealed class RecordingRepository : ISpotRepository
    {
        public AreaSpotFilter? AreaFilter { get; private set; }
        public NearbySpotFilter? NearbyFilter { get; private set; }

        public Task<IReadOnlyList<SpotModel>> SearchAreaAsync(AreaSpotFilter filter)
        {
            AreaFilter = filter;
            return Task.FromResult<IReadOnlyList<SpotModel>>([Spot(null)]);
        }

        public Task<IReadOnlyList<SpotModel>> FindNearbyAsync(NearbySpotFilter filter)
        {
            NearbyFilter = filter;
            return Task.FromResult<IReadOnlyList<SpotModel>>([Spot(120)]);
        }

        private static SpotModel Spot(double? distance) => new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"), "公園入口", "TW", "臺北市", "信義區",
            25.03, 121.52,
            [new DecorTypeModel("park", new Dictionary<string, string>
            {
                ["en"] = "Park", ["zh-TW"] = "公園"
            })], distance);
    }
}
