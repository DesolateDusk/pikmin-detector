using PikminDetector.Api.Common.Errors;
using PikminDetector.Api.Models;
using PikminDetector.Api.Repositories;
using PikminDetector.Api.Services;

namespace PikminDetector.Api.Tests;

public sealed class RecognitionServiceTests
{
    [Fact]
    public async Task RecognizeAsync_CompleteCafeCard_ReturnsOnlyMissingIcePikmin()
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
        Assert.Equal(new MissingPikminVM("cafe", "coffee_cup", "ice"), Assert.Single(cafe.Missing));
    }

    [Fact]
    public async Task RecognizeAsync_CompleteBakeryCard_ReportsMissingByCostumeAndType()
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
        Assert.Equal(
            [new MissingPikminVM("bakery", "baguette", "yellow"),
             new MissingPikminVM("bakery", "baguette", "blue"),
             new MissingPikminVM("bakery", "baguette", "white"),
             new MissingPikminVM("bakery", "baguette", "purple"),
             new MissingPikminVM("bakery", "baguette", "ice"),
             new MissingPikminVM("bakery", "pastry", "red"),
             new MissingPikminVM("bakery", "pastry", "yellow"),
             new MissingPikminVM("bakery", "pastry", "blue"),
             new MissingPikminVM("bakery", "pastry", "white"),
             new MissingPikminVM("bakery", "pastry", "purple"),
             new MissingPikminVM("bakery", "pastry", "rock"),
             new MissingPikminVM("bakery", "pastry", "winged"),
             new MissingPikminVM("bakery", "pastry", "ice")],
            bakery.Missing);
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
            [new MissingPikminVM("snowy_day", "snow", "blue"),
             new MissingPikminVM("snowy_day", "snow", "white"),
             new MissingPikminVM("snowy_day", "snow", "ice")],
            snow.Missing);
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
        Assert.Equal(new MissingPikminVM("cafe", "coffee_cup", "ice"), Assert.Single(cafe.Missing));
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
