using PikminDetector.Api.Common.Errors;
using PikminDetector.Api.Models;
using PikminDetector.Api.Repositories;
using PikminDetector.Api.Services;

namespace PikminDetector.Api.Tests;

public sealed class RecognitionServiceTests
{
    [Fact]
    public async Task RecognizeAsync_CompleteCafeCard_ReturnsEveryVisibleTypeWithStatus()
    {
        var service = new RecognitionService(new FixedCatalog(
            new CostumeCatalogRow("cafe", "coffee_cup", 1,
                ["red", "yellow", "blue", "white", "purple", "rock", "winged", "ice"],
                "Café", "咖啡廳"),
            new CostumeCatalogRow("sweetshop", "macaron", 1,
                ["red", "yellow", "blue", "white", "purple", "rock", "winged"],
                "Sweetshop", "甜點店")));
        await using var screenshot = OpenSample("S__126738455.jpg");

        var result = await service.RecognizeAsync(screenshot, "image/jpeg", screenshot.Length);

        var cafe = Assert.Single(result.Series);
        Assert.Equal("cafe", cafe.DecorTypeKey);
        Assert.Equal("咖啡廳", cafe.DecorTypeName);
        var costume = Assert.Single(cafe.Costumes);
        Assert.Equal("coffee_cup", costume.CostumeTypeKey);
        Assert.Equal(8, costume.AvailableTypes.Count);
        Assert.Equal(new RecognizedPikminVM("red", "collected"), costume.AvailableTypes[0]);
        Assert.Equal(new RecognizedPikminVM("ice", "missing"), costume.AvailableTypes[^1]);
    }

    [Fact]
    public async Task RecognizeAsync_CompleteBakeryCard_ReportsEveryTypeByCostume()
    {
        var service = new RecognitionService(new FixedCatalog(
            new CostumeCatalogRow("bakery", "baguette", 1,
                ["red", "yellow", "blue", "white", "purple", "rock", "winged", "ice"],
                "Bakery", "麵包店"),
            new CostumeCatalogRow("bakery", "pastry", 2,
                ["red", "yellow", "blue", "white", "purple", "rock", "winged", "ice"],
                "Bakery", "麵包店")));
        await using var screenshot = OpenSample("S__126738470_0.jpg");

        var result = await service.RecognizeAsync(screenshot, "image/jpeg", screenshot.Length);

        var bakery = Assert.Single(result.Series);
        Assert.Equal("bakery", bakery.DecorTypeKey);
        Assert.Collection(bakery.Costumes,
            baguette =>
            {
                Assert.Equal("baguette", baguette.CostumeTypeKey);
                Assert.Equal(8, baguette.AvailableTypes.Count);
                Assert.Equal(new RecognizedPikminVM("red", "collected"), baguette.AvailableTypes[0]);
                Assert.Equal(new RecognizedPikminVM("yellow", "missing"), baguette.AvailableTypes[1]);
            },
            pastry =>
            {
                Assert.Equal("pastry", pastry.CostumeTypeKey);
                Assert.Equal(8, pastry.AvailableTypes.Count);
                Assert.All(pastry.AvailableTypes, type => Assert.Equal("missing", type.Status));
            });
    }

    [Fact]
    public async Task RecognizeAsync_IncompleteRoadsideCard_SkipsSeries()
    {
        var service = new RecognitionService(new FixedCatalog(
            new CostumeCatalogRow("roadside", "coin", 1,
                ["red", "yellow", "blue", "white", "purple", "rock", "winged", "ice"],
                "Roadside", "路邊"),
            new CostumeCatalogRow("roadside", "green_sticker", 2,
                ["red", "yellow", "blue", "white", "purple", "rock", "winged", "ice"],
                "Roadside", "路邊"),
            new CostumeCatalogRow("roadside", "blue_sticker", 3,
                ["red", "yellow", "blue", "white", "purple", "rock", "winged", "ice"],
                "Roadside", "路邊")));
        await using var screenshot = OpenSample("S__126738474_0.jpg");

        var result = await service.RecognizeAsync(screenshot, "image/jpeg", screenshot.Length);

        Assert.Empty(result.Series);
    }

    [Fact]
    public async Task RecognizeAsync_CompleteSnowyDayCard_ReturnsThreeMissingTypes()
    {
        var service = new RecognitionService(new FixedCatalog(
            new CostumeCatalogRow("snowy_day", "snow", 1, ["blue", "white", "ice"],
                "Snowy Day", "下雪"),
            new CostumeCatalogRow("theme_park", "ferris_wheel_ticket", 1,
                ["red", "yellow", "blue"], "Theme Park", "主題樂園")));
        await using var screenshot = OpenSample("S__126738479_0.jpg");

        var result = await service.RecognizeAsync(screenshot, "image/jpeg", screenshot.Length);

        var snow = Assert.Single(result.Series, series => series.DecorTypeKey == "snowy_day");
        Assert.Equal(
            [new RecognizedPikminVM("blue", "missing"),
             new RecognizedPikminVM("white", "missing"),
             new RecognizedPikminVM("ice", "missing")],
            Assert.Single(snow.Costumes).AvailableTypes);
    }

    [Fact]
    public async Task RecognizeAsync_SyntheticEnglishCafeTitle_MatchesCatalogEnglishName()
    {
        var service = new RecognitionService(new FixedCatalog(
            new CostumeCatalogRow("cafe", "coffee_cup", 1,
                ["red", "yellow", "blue", "white", "purple", "rock", "winged", "ice"],
                "Cafe", "")));
        await using var screenshot = OpenSample("Cafe_English_synthetic.jpg");

        var result = await service.RecognizeAsync(screenshot, "image/jpeg", screenshot.Length);

        var cafe = Assert.Single(result.Series);
        Assert.Equal("cafe", cafe.DecorTypeKey);
        Assert.Equal(new RecognizedPikminVM("ice", "missing"), Assert.Single(cafe.Costumes).AvailableTypes[^1]);
    }

    [Fact]
    public async Task RecognizeAsync_EmptyImage_RejectsRequest()
    {
        var service = new RecognitionService(new FixedCatalog());
        await using var image = new MemoryStream();

        var error = await Assert.ThrowsAsync<AppException>(() =>
            service.RecognizeAsync(image, "image/jpeg", 0));

        Assert.Equal(400, error.StatusCode);
    }

    private static FileStream OpenSample(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(
                   Path.Combine(directory.FullName, "PikminDetector.Api", "recognition-samples")))
            directory = directory.Parent;
        var path = Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("recognition-samples"),
            "PikminDetector.Api", "recognition-samples", fileName);
        return File.OpenRead(path);
    }

    private sealed class FixedCatalog(params CostumeCatalogRow[] rows) : IRecognitionCatalogRepository
    {
        public Task<IReadOnlyList<CostumeCatalogRow>> GetAvailableCostumesAsync() =>
            Task.FromResult<IReadOnlyList<CostumeCatalogRow>>(rows);
    }
}
