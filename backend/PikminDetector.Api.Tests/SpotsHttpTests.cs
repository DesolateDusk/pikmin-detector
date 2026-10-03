using PikminDetector.Api.Models.Input;
using PikminDetector.Api.Models.View;
using PikminDetector.Api.Models.DbEntity;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PikminDetector.Api.Controllers;
using PikminDetector.Api.Repositories;

namespace PikminDetector.Api.Tests;

public sealed class SpotsHttpTests
{
    [Test]
    public async Task SearchArea_WithCountryCityAreaAndDecor_ForwardsFiltersAndReturnsDecorTypes()
    {
        var repository = new RecordingRepository();
        using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();

        var spots = await client.GetFromJsonAsync<SpotVM[]>(
            "/api/spots?country=TW&city=%E8%87%BA%E5%8C%97%E5%B8%82&area=%E4%BF%A1%E7%BE%A9%E5%8D%80&decorTypeKey=park");

        Assert.That(repository.AreaFilter, Is.Not.Null);
        Assert.That(repository.AreaFilter!.Country, Is.EqualTo("TW"));
        Assert.That(repository.AreaFilter.City, Is.EqualTo("臺北市"));
        Assert.That(repository.AreaFilter.Area, Is.EqualTo("信義區"));
        Assert.That(repository.AreaFilter.DecorTypeKey, Is.EqualTo("park"));
        Assert.That(repository.AreaFilter.Limit, Is.EqualTo(50));
        Assert.That(spots!, Has.Exactly(1).Items);
        var spot = spots!.Single();
        Assert.That(spot.Country, Is.EqualTo("TW"));
        Assert.That(spot.DecorTypes.Single().Key, Is.EqualTo("park"));
    }

    [Test]
    public async Task SearchArea_WithoutCountry_ReturnsValidationProblem()
    {
        using var factory = CreateFactory(new RecordingRepository());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/spots?city=Taipei");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
    }

    [Test]
    public async Task SearchArea_AreaWithoutCity_ReturnsValidationProblem()
    {
        using var factory = CreateFactory(new RecordingRepository());
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/spots?country=TW&area=%E4%BF%A1%E7%BE%A9%E5%8D%80");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task FindNearby_WithRadiusAndDecor_ForwardsFiltersAndReturnsDistance()
    {
        var repository = new RecordingRepository();
        using var factory = CreateFactory(repository);
        using var client = factory.CreateClient();

        var spots = await client.GetFromJsonAsync<SpotVM[]>(
            "/api/spots/nearby?latitude=25.03&longitude=121.52&radiusMeters=500&decorTypeKey=cafe");

        Assert.That(repository.NearbyFilter, Is.Not.Null);
        Assert.That(repository.NearbyFilter!.Latitude, Is.EqualTo(25.03));
        Assert.That(repository.NearbyFilter.Longitude, Is.EqualTo(121.52));
        Assert.That(repository.NearbyFilter.RadiusMeters, Is.EqualTo(500));
        Assert.That(repository.NearbyFilter.DecorTypeKey, Is.EqualTo("cafe"));
        Assert.That(repository.NearbyFilter.Limit, Is.EqualTo(50));
        Assert.That(spots!.Single().DistanceMeters, Is.EqualTo(120));
    }
    [TestCase("/api/spots/nearby?latitude=91&longitude=121")]
    [TestCase("/api/spots/nearby?latitude=25&longitude=181")]
    [TestCase("/api/spots/nearby?latitude=25&longitude=121&radiusMeters=0")]
    [TestCase("/api/spots/nearby?latitude=25&longitude=121&limit=101")]
    public async Task FindNearby_InvalidNumericInput_ReturnsValidationProblem(string path)
    {
        using var factory = CreateFactory(new RecordingRepository());
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
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
        public AreaSpotInput? AreaFilter { get; private set; }
        public NearbySpotInput? NearbyFilter { get; private set; }

        public Task<IReadOnlyList<SpotModel>> SearchAreaAsync(AreaSpotInput filter)
        {
            AreaFilter = filter;
            return Task.FromResult<IReadOnlyList<SpotModel>>([Spot(null)]);
        }

        public Task<IReadOnlyList<SpotModel>> FindNearbyAsync(NearbySpotInput filter)
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
