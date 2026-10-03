using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PikminDetector.Api.Controllers;
using PikminDetector.Api.Models;
using PikminDetector.Api.Services;

namespace PikminDetector.Api.Tests;

public sealed class RecognitionsHttpTests
{
    [Fact]
    public async Task Recognize_ReturnsSeriesCostumeAndEveryAvailableType()
    {
        using var factory = new WebApplicationFactory<RecognitionsController>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRecognitionService>();
                services.AddSingleton<IRecognitionService>(new FixedRecognition());
            }));
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent([1, 2, 3]), "image", "collection.jpg");

        var response = await client.PostAsync("/api/recognitions", form);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<RecognitionVM>();

        var series = Assert.Single(payload!.Series);
        Assert.Equal("cafe", series.DecorTypeKey);
        Assert.Equal("咖啡廳", series.DecorTypeName);
        var costume = Assert.Single(series.Costumes);
        Assert.Equal("coffee_cup", costume.CostumeTypeKey);
        Assert.Equal("咖啡杯", costume.CostumeTypeName);
        Assert.Equal(
            [new RecognizedPikminVM("red", "collected"), new RecognizedPikminVM("ice", "missing")],
            costume.AvailableTypes);
    }

    private sealed class FixedRecognition : IRecognitionService
    {
        public Task<RecognitionVM> RecognizeAsync(Stream image, string contentType, long length) =>
            Task.FromResult(new RecognitionVM([
                new RecognizedSeriesVM("cafe", "咖啡廳", [
                    new RecognizedCostumeVM("coffee_cup", "咖啡杯", [
                        new RecognizedPikminVM("red", "collected"),
                        new RecognizedPikminVM("ice", "missing")])])
            ]));
    }
}
