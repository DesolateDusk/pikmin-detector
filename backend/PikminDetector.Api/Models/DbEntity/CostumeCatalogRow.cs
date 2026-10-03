namespace PikminDetector.Api.Models.DbEntity;

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
