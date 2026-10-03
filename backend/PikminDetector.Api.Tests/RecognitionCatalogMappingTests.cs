using PikminDetector.Api.Models.DbEntity;
using System.Data;
using Dapper;

namespace PikminDetector.Api.Tests;

public sealed class RecognitionCatalogMappingTests
{
    [Test]
    public void CostumeCatalogRow_DapperMapsArrayColumnAndNames()
    {
        var table = new DataTable();
        table.Columns.Add("DecorTypeKey", typeof(string));
        table.Columns.Add("CostumeTypeKey", typeof(string));
        table.Columns.Add("DisplayOrder", typeof(int));
        table.Columns.Add("AvailableTypes", typeof(Array));
        table.Columns.Add("DecorNameEn", typeof(string));
        table.Columns.Add("DecorNameZh", typeof(string));
        table.Columns.Add("CostumeNameEn", typeof(string));
        table.Columns.Add("CostumeNameZh", typeof(string));
        table.Rows.Add("cafe", "coffee_cup", 1, new[] { "red", "ice" }, "Cafe", "咖啡廳", "Coffee Cup", "咖啡杯");
        using var reader = table.CreateDataReader();
        Assert.That(reader.Read(), Is.True);

        var catalogRow = reader.GetRowParser<CostumeCatalogRow>()(reader);

        Assert.That(catalogRow.DecorTypeKey, Is.EqualTo("cafe"));
        Assert.That(catalogRow.CostumeTypeKey, Is.EqualTo("coffee_cup"));
        Assert.That(catalogRow.AvailableTypes, Is.EqualTo(new[] { "red", "ice" }));
        Assert.That(catalogRow.DecorNameZh, Is.EqualTo("咖啡廳"));
        Assert.That(catalogRow.CostumeNameZh, Is.EqualTo("咖啡杯"));
    }
}
