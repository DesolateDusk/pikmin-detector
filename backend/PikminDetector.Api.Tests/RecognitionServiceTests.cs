using PikminDetector.Api.Models.View;
using PikminDetector.Api.Models.DbEntity;
using PikminDetector.Api.Lib.CustomException;
using PikminDetector.Api.Repositories;
using PikminDetector.Api.Services;

namespace PikminDetector.Api.Tests;

public sealed class RecognitionServiceTests
{
    private static readonly string[] SevenTypes = ["red", "yellow", "blue", "white", "purple", "rock", "winged"];
    private static readonly string[] EightTypes = [.. SevenTypes, "ice"];

    [Test]
    public async Task RecognizeAsync_CompleteCornerStoreCard_ReturnsBothFullyCollectedCostumes()
    {
        var service = new RecognitionService(new FixedCatalog(
            Row("corner_store", "bottle_cap", 1, SevenTypes, "Corner Store", "便利店"),
            Row("corner_store", "snack", 2, SevenTypes, "Corner Store", "便利店")));
        await using var screenshot = OpenSample("S__126738468_0.jpg");

        var result = await service.RecognizeAsync(screenshot, "image/jpeg", screenshot.Length);

        Assert.That(result.Series, Has.Exactly(1).Items);
        var store = result.Series.Single();
        Assert.That(store.DecorTypeKey, Is.EqualTo("corner_store"));
        Assert.That(store.DecorTypeName, Is.EqualTo("便利店"));
        Assert.That(store.Costumes, Has.Count.EqualTo(2));
        AssertCostume(store.Costumes[0], "bottle_cap", SevenTypes,
                "collected", "collected", "collected", "collected", "collected", "collected", "collected");
        AssertCostume(store.Costumes[1], "snack", SevenTypes,
                "collected", "collected", "collected", "collected", "collected", "collected", "collected");
    }

    [Test]
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

        Assert.That(result.Series, Has.Exactly(1).Items);
        var bakery = result.Series.Single();
        Assert.That(bakery.DecorTypeKey, Is.EqualTo("bakery"));
        Assert.That(bakery.Costumes, Has.Count.EqualTo(2));
        {
                Assert.That(bakery.Costumes[0].CostumeTypeKey, Is.EqualTo("baguette"));
                Assert.That(bakery.Costumes[0].AvailableTypes.Count, Is.EqualTo(8));
                Assert.That(bakery.Costumes[0].AvailableTypes[0], Is.EqualTo(new RecognizedPikminVM("red", "collected")));
                Assert.That(bakery.Costumes[0].AvailableTypes[1], Is.EqualTo(new RecognizedPikminVM("yellow", "missing")));
            }
        {
                Assert.That(bakery.Costumes[1].CostumeTypeKey, Is.EqualTo("pastry"));
                Assert.That(bakery.Costumes[1].AvailableTypes.Count, Is.EqualTo(8));
                foreach (var item in bakery.Costumes[1].AvailableTypes)
                Assert.That(item.Status, Is.EqualTo("missing"));
            }
    }

    [Test]
    public async Task RecognizeAsync_CompleteParkCard_ReturnsCollectedCloverAndMixedFourLeafClover()
    {
        var service = new RecognitionService(new FixedCatalog(
            Row("park", "clover", 1, EightTypes, "Park", "公園"),
            Row("park", "four_leaf_clover", 2, EightTypes, "Park", "公園")));
        await using var screenshot = OpenSample("S__126738472(1).jpg");

        var result = await service.RecognizeAsync(screenshot, "image/jpeg", screenshot.Length);

        Assert.That(result.Series, Has.Exactly(1).Items);
        var park = result.Series.Single();
        Assert.That(park.DecorTypeKey, Is.EqualTo("park"));
        Assert.That(park.Costumes, Has.Count.EqualTo(2));
        AssertCostume(park.Costumes[0], "clover", EightTypes,
                "collected", "collected", "collected", "collected", "collected", "collected", "collected", "collected");
        AssertCostume(park.Costumes[1], "four_leaf_clover", EightTypes,
                "missing", "missing", "collected", "missing", "collected", "missing", "collected", "collected");
    }

    [Test]
    public async Task RecognizeAsync_CompleteSupermarketCard_ReturnsMixedStatusesForBothCostumes()
    {
        var service = new RecognitionService(new FixedCatalog(
            Row("supermarket", "mushroom", 1, SevenTypes, "Supermarket", "超市"),
            Row("supermarket", "banana", 2, SevenTypes, "Supermarket", "超市")));
        await using var screenshot = OpenSample("S__126738469_0.jpg");

        var result = await service.RecognizeAsync(screenshot, "image/jpeg", screenshot.Length);

        Assert.That(result.Series, Has.Exactly(1).Items);
        var supermarket = result.Series.Single();
        Assert.That(supermarket.DecorTypeKey, Is.EqualTo("supermarket"));
        Assert.That(supermarket.Costumes, Has.Count.EqualTo(2));
        AssertCostume(supermarket.Costumes[0], "mushroom", SevenTypes,
                "missing", "collected", "missing", "collected", "missing", "collected", "missing");
        AssertCostume(supermarket.Costumes[1], "banana", SevenTypes,
                "missing", "collected", "missing", "collected", "missing", "collected", "collected");
    }

    [Test]
    public async Task RecognizeAsync_HairSalonAndClothesCards_ReturnsEachSeriesWithItsOwnStatuses()
    {
        var service = new RecognitionService(new FixedCatalog(
            Row("hair_salon", "scissors", 1, SevenTypes, "Hair Salon", "美容院"),
            Row("clothes_store", "hair_tie", 1, SevenTypes, "Clothes Store", "服裝店")));
        await using var screenshot = OpenSample("S__126738471_0.jpg");

        var result = await service.RecognizeAsync(screenshot, "image/jpeg", screenshot.Length);

        Assert.That(result.Series.Count, Is.EqualTo(2));
        var salon = result.Series.Single(series => series.DecorTypeKey == "hair_salon");
        AssertCostume(salon.Costumes.Single(), "scissors", SevenTypes,
            "missing", "missing", "missing", "missing", "missing", "missing", "collected");
        var clothes = result.Series.Single(series => series.DecorTypeKey == "clothes_store");
        AssertCostume(clothes.Costumes.Single(), "hair_tie", SevenTypes,
            "collected", "collected", "collected", "collected", "missing", "collected", "missing");
    }

    [Test]
    public async Task RecognizeAsync_EmptyImage_RejectsRequest()
    {
        var service = new RecognitionService(new FixedCatalog());
        await using var image = new MemoryStream();

        var error = Assert.ThrowsAsync<CommonException>(() =>
            service.RecognizeAsync(image, "image/jpeg", 0));

        Assert.That(error!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task RecognizeAsync_IncompleteBakeryCard_SkipsSeries()
    {
        var service = new RecognitionService(new FixedCatalog(
            Row("bakery", "baguette", 1, EightTypes, "Bakery", "麵包店"),
            Row("bakery", "pastry", 2, EightTypes, "Bakery", "麵包店")));
        await using var screenshot = OpenSample("S__126738469_0.jpg");

        var result = await service.RecognizeAsync(screenshot, "image/jpeg", screenshot.Length);

        Assert.That(result.Series, Is.Empty);
    }

    private static CostumeCatalogRow Row(
        string decorKey, string costumeKey, int order, string[] types, string englishName, string chineseName)
        => new(decorKey, costumeKey, order, types, englishName, chineseName);

    private static void AssertCostume(
        RecognizedCostumeVM costume, string key, string[] types, params string[] statuses)
    {
        Assert.That(costume.CostumeTypeKey, Is.EqualTo(key));
        Assert.That(statuses.Length, Is.EqualTo(types.Length));
        Assert.That(costume.AvailableTypes, Is.EqualTo(types.Select((type, index) => new RecognizedPikminVM(type, statuses[index]))));
    }

    private static FileStream OpenSample(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(
                   Path.Combine(directory.FullName, "test-image")))
            directory = directory.Parent;
        var path = Path.Combine(directory?.FullName ?? throw new DirectoryNotFoundException("test-image"),
            "test-image", fileName);
        return File.OpenRead(path);
    }

    private sealed class FixedCatalog(params CostumeCatalogRow[] rows) : IRecognitionCatalogRepository
    {
        public Task<IReadOnlyList<CostumeCatalogRow>> GetAvailableCostumesAsync() =>
            Task.FromResult<IReadOnlyList<CostumeCatalogRow>>(rows);
    }
}
