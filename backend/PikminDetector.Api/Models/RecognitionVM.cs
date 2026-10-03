namespace PikminDetector.Api.Models;

public sealed record RecognitionVM(IReadOnlyList<RecognizedSeriesVM> Series);

public sealed record RecognizedSeriesVM(
    string DecorTypeKey,
    string DecorTypeName,
    IReadOnlyList<RecognizedCostumeVM> Costumes);

public sealed record RecognizedCostumeVM(
    string CostumeTypeKey,
    string CostumeTypeName,
    IReadOnlyList<RecognizedPikminVM> AvailableTypes);

public sealed record RecognizedPikminVM(string PikminType, string Status);

public sealed record CostumeCatalogRow(
    string DecorTypeKey,
    string CostumeTypeKey,
    int DisplayOrder,
    string[] AvailableTypes,
    string DecorNameEn = "",
    string DecorNameZh = "",
    string CostumeNameEn = "",
    string CostumeNameZh = "")
{
    public CostumeCatalogRow() : this("", "", 0, [], "", "", "", "") { }
}
