using Dapper;
using Npgsql;
using PikminDetector.Api.Common.Errors;
using PikminDetector.Api.Models;

namespace PikminDetector.Api.Repositories;

public interface IRecognitionCatalogRepository
{
    Task<IReadOnlyList<CostumeCatalogRow>> GetAvailableCostumesAsync();
}

public sealed class RecognitionCatalogRepository(IConfiguration configuration) : IRecognitionCatalogRepository
{
    public async Task<IReadOnlyList<CostumeCatalogRow>> GetAvailableCostumesAsync()
    {
        var connectionString = configuration.GetConnectionString("Pikmin");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new AppException(StatusCodes.Status503ServiceUnavailable, "Spot data service is not configured.");

        const string sql = """
            select d.key as DecorTypeKey, c.key as CostumeTypeKey,
                   c.display_order as DisplayOrder,
                   c.available_types as AvailableTypes,
                   d.name->>'en' as DecorNameEn,
                   d.name->>'zh-TW' as DecorNameZh
            from pikmin.costume_type c
            join pikmin.decor_type d on d.id = c.decor_id
            where c.available_types is not null
            order by d.display_order, c.display_order
            """;
        await using var connection = new NpgsqlConnection(connectionString);
        var rows = await connection.QueryAsync<CostumeCatalogRow>(sql);
        return rows.ToArray();
    }
}
