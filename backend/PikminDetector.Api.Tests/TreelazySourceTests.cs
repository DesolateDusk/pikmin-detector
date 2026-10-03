using PikminDetector.Api.Models.DbEntity;
using System.Net;
using System.Text;
using PikminDetector.Api.Services;

namespace PikminDetector.Api.Tests;

public sealed class TreelazySourceTests
{
    [Test]
    public async Task GetTaiwanAreaAsync_MixedAndPureRecords_ImportsOnlyPureWithPathGeography()
    {
        using var handler = new SourceHandler("""
            [
              {"id":"pure-1","name":"公園","lat":25.03,"lon":121.52,"decorTypes":["park"],"status":"pure"},
              {"id":"mixed-1","name":"路口","lat":25.04,"lon":121.53,"decorTypes":["park","roadside"],"status":"mixed"}
            ]
            """);
        using var client = new HttpClient(handler);

        var batch = await new TreelazySource(client).GetTaiwanAreaAsync("臺北市", "信義區");

        Assert.That(handler.LastUri, Is.EqualTo("https://treelazy.com/pikmin/data/%E8%87%BA%E5%8C%97%E5%B8%82/%E4%BF%A1%E7%BE%A9%E5%8D%80.json"));
        Assert.That(batch.SourceCount, Is.EqualTo(2));
        Assert.That(batch.Spots, Has.Exactly(1).Items);
        var spot = batch.Spots.Single();
        Assert.That(spot.SourceId, Is.EqualTo("pure-1"));
        Assert.That(spot.Country, Is.EqualTo("TW"));
        Assert.That(spot.City, Is.EqualTo("臺北市"));
        Assert.That(spot.Area, Is.EqualTo("信義區"));
        Assert.That(spot.DecorKeys.Single(), Is.EqualTo("park"));
    }

    [Test]
    public async Task GetCountryAsync_JapanCity_FiltersCountryFileByRecordCity()
    {
        using var handler = new SourceHandler("""
            [
              {"id":"tokyo-1","name":"Roadside","city":"東京23区","lat":35.7,"lon":139.7,"decorTypes":["roadside"],"status":"pure"},
              {"id":"osaka-1","name":"Park","city":"大阪市","lat":34.7,"lon":135.5,"decorTypes":["park"],"status":"pure"}
            ]
            """);
        using var client = new HttpClient(handler);

        var batch = await new TreelazySource(client)
            .GetCountryAsync(new SpotImportScope("JP", "東京23区", null));

        Assert.That(handler.LastUri, Is.EqualTo("https://treelazy.com/pikmin/data/japan.json"));
        Assert.That(batch.SourceCount, Is.EqualTo(1));
        Assert.That(batch.Spots, Has.Exactly(1).Items);
        var spot = batch.Spots.Single();
        Assert.That(spot.Country, Is.EqualTo("JP"));
        Assert.That(spot.City, Is.EqualTo("東京23区"));
        Assert.That(spot.Area, Is.Null);
    }

    [Test]
    public async Task GetCountryAsync_NoPureRecords_ReturnsEmptyBatch()
    {
        using var handler = new SourceHandler("""
            [{"id":"mixed-1","name":"Mixed","city":"東京23区","lat":35.7,"lon":139.7,"decorTypes":["park","forest"],"status":"mixed"}]
            """);
        using var client = new HttpClient(handler);

        var batch = await new TreelazySource(client)
            .GetCountryAsync(new SpotImportScope("JP", "東京23区", null));

        Assert.That(batch.SourceCount, Is.EqualTo(1));
        Assert.That(batch.Spots, Is.Empty);
    }

    [Test]
    public async Task GetCountryAsync_CountryScope_PreservesCityFromEachRecord()
    {
        using var handler = new SourceHandler("""
            [
              {"id":"tokyo-1","name":"A","city":"東京23区","lat":35.7,"lon":139.7,"decorTypes":["park"],"status":"pure"},
              {"id":"no-city","name":"B","lat":35.8,"lon":139.8,"decorTypes":["park"],"status":"pure"}
            ]
            """);
        using var client = new HttpClient(handler);

        var batch = await new TreelazySource(client)
            .GetCountryAsync(new SpotImportScope("JP", null, null));

        Assert.That(batch.Spots[0].City, Is.EqualTo("東京23区"));
        Assert.That(batch.Spots[1].City, Is.Null);
    }

    [Test]
    public async Task GetCountryAsync_UnknownCity_RejectsBeforeImport()
    {
        using var handler = new SourceHandler("""
            [{"id":"tokyo-1","name":"A","city":"東京23区","lat":35.7,"lon":139.7,"decorTypes":["park"],"status":"pure"}]
            """);
        using var client = new HttpClient(handler);

        Assert.ThrowsAsync<InvalidDataException>(() => new TreelazySource(client)
            .GetCountryAsync(new SpotImportScope("JP", "大阪市", null)));
    }

    [Test]
    public async Task GetCountryAsync_PureRecordWithTwoDecorTypes_RejectsAmbiguousSource()
    {
        using var handler = new SourceHandler("""
            [{"id":"ambiguous","name":"A","city":"東京23区","lat":35.7,"lon":139.7,"decorTypes":["park","forest"],"status":"pure"}]
            """);
        using var client = new HttpClient(handler);

        Assert.ThrowsAsync<InvalidDataException>(() => new TreelazySource(client)
            .GetCountryAsync(new SpotImportScope("JP", "東京23区", null)));
    }

    [Test]
    public async Task GetAreasAsync_TaipeiPage_ReturnsAreasOnce()
    {
        using var handler = new SourceHandler("""
            <a href="/pikmin/taipei/xinyi">信義區</a><a href="/pikmin/taipei/shilin">士林區</a>
            <a href="/pikmin/taipei/xinyi">信義區</a>
            """);
        using var client = new HttpClient(handler);

        var areas = await new TreelazySource(client).GetAreasAsync("臺北市");

        Assert.That(handler.LastUri, Is.EqualTo("https://treelazy.com/pikmin/taipei"));
        Assert.That(areas, Is.EqualTo(new[] { "信義區", "士林區" }));
    }

    private sealed class SourceHandler(string body) : HttpMessageHandler
    {
        public string? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri?.AbsoluteUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
        }
    }
}
