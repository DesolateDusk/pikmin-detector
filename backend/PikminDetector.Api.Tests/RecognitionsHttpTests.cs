using PikminDetector.Api.Models.View;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PikminDetector.Api.Controllers;
using PikminDetector.Api.Services;

namespace PikminDetector.Api.Tests;

public sealed class RecognitionsHttpTests
{
    [Test]
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

        Assert.That(payload!.Series, Has.Exactly(1).Items);
        var series = payload!.Series.Single();
        Assert.That(series.DecorTypeKey, Is.EqualTo("cafe"));
        Assert.That(series.DecorTypeName, Is.EqualTo("咖啡廳"));
        Assert.That(series.Costumes, Has.Exactly(1).Items);
        var costume = series.Costumes.Single();
        Assert.That(costume.CostumeTypeKey, Is.EqualTo("coffee_cup"));
        Assert.That(costume.CostumeTypeName, Is.EqualTo("咖啡杯"));
        Assert.That(costume.AvailableTypes, Is.EqualTo(new[] { new RecognizedPikminVM("red", "collected"), new RecognizedPikminVM("ice", "missing") }));
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
