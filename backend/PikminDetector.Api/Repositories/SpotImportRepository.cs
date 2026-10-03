using PikminDetector.Api.Models.DbEntity;
using PikminDetector.Api.Models.Enum;
using System.Data;
using System.Text.Json;
using Dapper;

namespace PikminDetector.Api.Repositories;

public sealed class SpotImportRepository(IDbContext context) : ISpotImportRepository
{
    // This staging table exists only on this connection until the transaction commits.
    private const string StageSql = """
        create temp table import_spots on commit drop as
        select source_id, name, country, city, area, lat, lon, decor_keys
        from jsonb_to_recordset(@Spots::jsonb) as x(
            source_id text, name text, country text, city text, area text,
            lat double precision, lon double precision, decor_keys jsonb
        );
        create unique index on import_spots(lat, lon);
        """;

    private const string MissingKeysSql = """
        select distinct k.key from import_spots i
        cross join lateral jsonb_array_elements_text(i.decor_keys) as k(key)
        left join pikmin.decor_type d on d.key = k.key
        where d.id is null
        """;

    private const string ConflictingSpotsSql = """
        select count(*) from import_spots i
        join pikmin.spot s on s.latitude = i.lat and s.longitude = i.lon
        where s.source_id is null or s.source_id not like 'treelazy:%'
        """;

    private const string InsertSpotsSql = """
        insert into pikmin.spot (source_id, name, country, city, area, latitude, longitude)
        select source_id, name, country, city, area, lat, lon from import_spots
        on conflict (latitude, longitude) do nothing
        returning id
        """;

    private const string UpdateSpotsSql = """
        update pikmin.spot s
        set source_id = i.source_id, name = i.name,
            country = i.country, city = i.city, area = i.area
        from import_spots i
        where s.latitude = i.lat and s.longitude = i.lon
          and s.source_id like 'treelazy:%'
          and (s.source_id, s.name, s.country, s.city, s.area)
              is distinct from (i.source_id, i.name, i.country, i.city, i.area)
        returning s.id
        """;

    private const string DeleteSpotDetectorsSql = """
        delete from pikmin.spot_detector sd
        using pikmin.spot s, import_spots i, pikmin.decor_type d
        where sd.spot_id = s.id and sd.decor_id = d.id
          and s.latitude = i.lat and s.longitude = i.lon
          and s.source_id like 'treelazy:%'
          and not exists (select 1
            from jsonb_array_elements_text(i.decor_keys) as k(key)
            where k.key = d.key)
        returning sd.spot_id
        """;

    private const string InsertSpotDetectorsSql = """
        insert into pikmin.spot_detector (spot_id, decor_id)
        select s.id, d.id from import_spots i
        join pikmin.spot s on s.latitude = i.lat and s.longitude = i.lon
        cross join lateral jsonb_array_elements_text(i.decor_keys) as k(key)
        join pikmin.decor_type d on d.key = k.key
        on conflict (spot_id, decor_id) do nothing
        returning spot_id
        """;

    private const string DeleteObsoleteSpotDetectorsSql = """
        delete from pikmin.spot_detector sd
        using pikmin.spot s
        where sd.spot_id = s.id and s.source_id like 'treelazy:%' and s.country = @Country
          and (@City is null or s.city = @City)
          and (@Area is null or s.area = @Area)
          and not exists (select 1 from import_spots i
            where i.lat = s.latitude and i.lon = s.longitude)
        """;

    private const string DeleteSpotsSql = """
        delete from pikmin.spot s
        where s.source_id like 'treelazy:%' and s.country = @Country
          and (@City is null or s.city = @City)
          and (@Area is null or s.area = @Area)
          and not exists (select 1 from import_spots i
            where i.lat = s.latitude and i.lon = s.longitude)
        returning s.id
        """;

    public async Task<SpotImportCounts> SynchronizeAsync(SpotImportBatch batch)
    {
        var payload = JsonSerializer.Serialize(batch.Spots.Select(spot => new
        {
            source_id = $"treelazy:{spot.SourceId}",
            name = spot.Name, country = spot.Country, city = spot.City,
            area = spot.Area, lat = spot.Latitude, lon = spot.Longitude,
            decor_keys = spot.DecorKeys
        }));
        var parameters = new DynamicParameters();
        parameters.Add("Spots", payload, DbType.String);
        parameters.Add("Country", batch.Scope.Country, DbType.String);
        parameters.Add("City", batch.Scope.City, DbType.String);
        parameters.Add("Area", batch.Scope.Area, DbType.String);

        await using var connection = context.CreateConnection(ConnectionKeys.PostgreSql);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await connection.ExecuteAsync(new CommandDefinition(StageSql, parameters, transaction, commandTimeout: 120));
        var conflicts = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(ConflictingSpotsSql, transaction: transaction));
        if (conflicts > 0)
        {
            throw new InvalidDataException("Source coordinates conflict with a non-Treelazy spot.");
        }
        var missingKeys = (await connection.QueryAsync<string>(
            new CommandDefinition(MissingKeysSql, transaction: transaction))).ToArray();
        if (missingKeys.Length > 0)
        {
            throw new InvalidDataException($"Decor keys missing from database: {string.Join(", ", missingKeys)}.");
        }

        async Task<Guid[]> ChangedIdsAsync(string sql) =>
            (await connection.QueryAsync<Guid>(
                new CommandDefinition(sql, parameters, transaction, commandTimeout: 120))).ToArray();

        var addedIds = await ChangedIdsAsync(InsertSpotsSql);
        var updatedIds = (await ChangedIdsAsync(UpdateSpotsSql)).ToHashSet();
        updatedIds.UnionWith(await ChangedIdsAsync(DeleteSpotDetectorsSql));
        updatedIds.UnionWith(await ChangedIdsAsync(InsertSpotDetectorsSql));
        updatedIds.ExceptWith(addedIds);
        await connection.ExecuteAsync(new CommandDefinition(
            DeleteObsoleteSpotDetectorsSql, parameters, transaction, commandTimeout: 120));
        var removedIds = await ChangedIdsAsync(DeleteSpotsSql);
        await transaction.CommitAsync();
        return new SpotImportCounts(addedIds.Length, updatedIds.Count, removedIds.Length);
    }
}
