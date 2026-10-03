using PikminDetector.Api.Models.DbEntity;
using PikminDetector.Api.Models.Enum;
using Dapper;

namespace PikminDetector.Api.Repositories;

public sealed class RecognitionCatalogRepository(IDbContext context) : IRecognitionCatalogRepository
{
    public async Task<IReadOnlyList<CostumeCatalogRow>> GetAvailableCostumesAsync()
    {
        const string sql = """
            select d.key as DecorTypeKey, c.key as CostumeTypeKey,
                   c.display_order as DisplayOrder,
                   c.available_types as AvailableTypes,
                   d.name->>'en' as DecorNameEn,
                   d.name->>'zh-TW' as DecorNameZh,
                   c.name->>'en' as CostumeNameEn,
                   c.name->>'zh-TW' as CostumeNameZh
            from pikmin.costume_type c
            join pikmin.decor_type d on d.id = c.decor_id
            where c.available_types is not null
            order by d.display_order, c.display_order
            """;
        // The upstream transaction pooler can leave client-pooled connections unable to read the next query.
        await using var connection = context.CreateConnection(ConnectionKeys.PostgreSql, pooling: false);
        var rows = await connection.QueryAsync<CostumeCatalogRow>(sql);
        return rows.ToArray();
    }
}
