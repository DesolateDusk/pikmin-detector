using System.Data;
using Dapper;
using PikminDetector.Api.Models;

namespace PikminDetector.Api.Tests;

public sealed class RecognitionCatalogMappingTests
{
    [Fact]
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
        Assert.True(reader.Read());

        var catalogRow = reader.GetRowParser<CostumeCatalogRow>()(reader);

        Assert.Equal("cafe", catalogRow.DecorTypeKey);
        Assert.Equal("coffee_cup", catalogRow.CostumeTypeKey);
        Assert.Equal(["red", "ice"], catalogRow.AvailableTypes);
        Assert.Equal("咖啡廳", catalogRow.DecorNameZh);
        Assert.Equal("咖啡杯", catalogRow.CostumeNameZh);
    }
}
