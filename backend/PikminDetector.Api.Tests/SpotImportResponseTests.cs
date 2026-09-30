using System.Text.Json;
using PikminDetector.Api.Models;

namespace PikminDetector.Api.Tests;

public sealed class SpotImportResponseTests
{
    [Fact]
    public void SpotImportVM_WebJson_ContainsOnlyCountsAndFailures()
    {
        var response = new SpotImportVM(2, 3, 1,
            [new SpotImportFailure("臺北市", "信義區", "Source unavailable")]);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(
            response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        Assert.Equal(["added", "updated", "removed", "failures"],
            document.RootElement.EnumerateObject().Select(property => property.Name));
        Assert.Equal(2, document.RootElement.GetProperty("added").GetInt32());
        Assert.Equal(3, document.RootElement.GetProperty("updated").GetInt32());
        Assert.Equal(1, document.RootElement.GetProperty("removed").GetInt32());
        Assert.Single(document.RootElement.GetProperty("failures").EnumerateArray());
    }
}
