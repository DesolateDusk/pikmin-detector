namespace PikminDetector.Api.Models;

public sealed record RecognitionVM(IReadOnlyList<RecognizedSeriesVM> Series);

public sealed record RecognizedSeriesVM(
    string DecorTypeKey,
    IReadOnlyList<MissingPikminVM> Missing);

public sealed record MissingPikminVM(
    string DecorTypeKey,
    string CostumeTypeKey,
    string PikminType);

public sealed record CostumeCatalogRow(
    string DecorTypeKey,
    string CostumeTypeKey,
    int DisplayOrder,
    string[] AvailableTypes,
    string DecorNameEn = "",
    string DecorNameZh = "");
