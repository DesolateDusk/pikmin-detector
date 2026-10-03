using PikminDetector.Api.Models.View;
using System.Text.Json;

namespace PikminDetector.Api.Tests;

public sealed class SpotImportResponseTests
{
    [Test]
    public void SpotImportVM_WebJson_ContainsOnlyCountsAndFailures()
    {
        var response = new SpotImportVM(2, 3, 1,
            [new SpotImportFailure("臺北市", "信義區", "Source unavailable")]);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(
            response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        Assert.That(document.RootElement.EnumerateObject().Select(property => property.Name), Is.EqualTo(new[] { "added", "updated", "removed", "failures" }));
        Assert.That(document.RootElement.GetProperty("added").GetInt32(), Is.EqualTo(2));
        Assert.That(document.RootElement.GetProperty("updated").GetInt32(), Is.EqualTo(3));
        Assert.That(document.RootElement.GetProperty("removed").GetInt32(), Is.EqualTo(1));
        Assert.That(document.RootElement.GetProperty("failures").EnumerateArray(), Has.Exactly(1).Items);
    }
}
