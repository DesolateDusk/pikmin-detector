using System.Text.Json;
using Dapper;
using Npgsql;
using PikminDetector.Api.Common.Errors;
using PikminDetector.Api.Models;

namespace PikminDetector.Api.Repositories;

public sealed class SpotRepository(IConfiguration configuration) : ISpotRepository
{
    private const string AreaSql = """
        with selected_spots as (
            select s.id, s.name, s.country, s.city, s.area, s.latitude, s.longitude,
                   null::double precision as distance_meters
            from pikmin.spot s
            where s.country = @Country
              and (@City is null or s.city = @City)
              and (@Area is null or s.area = @Area)
              and (@DecorTypeKey is null or exists (
                  select 1 from pikmin.spot_detector sd
                  join pikmin.decor_type d on d.id = sd.decor_id
                  where sd.spot_id = s.id and d.key = @DecorTypeKey
              ))
            order by s.name, s.id
            limit @Limit
        )
        select s.id as Id, s.name as Name, s.country as Country, s.city as City,
               s.area as Area, s.latitude as Latitude, s.longitude as Longitude,
               s.distance_meters as DistanceMeters, d.key as DecorKey,
               d.name::text as DecorName
        from selected_spots s
        join pikmin.spot_detector sd on sd.spot_id = s.id
        join pikmin.decor_type d on d.id = sd.decor_id
        order by s.name, s.id, d.key
        """;

    private const string NearbySql = """
        with selected_spots as (
            select s.id, s.name, s.country, s.city, s.area, s.latitude, s.longitude,
                   gis.st_distance(s.location, q.position) as distance_meters
            from pikmin.spot s
            cross join lateral (
                select gis.st_setsrid(gis.st_makepoint(@Longitude, @Latitude), 4326)::gis.geography as position
            ) q
            where gis.st_dwithin(s.location, q.position, @RadiusMeters)
              and (@DecorTypeKey is null or exists (
                  select 1 from pikmin.spot_detector sd
                  join pikmin.decor_type d on d.id = sd.decor_id
                  where sd.spot_id = s.id and d.key = @DecorTypeKey
              ))
            order by distance_meters, s.id
            limit @Limit
        )
        select s.id as Id, s.name as Name, s.country as Country, s.city as City,
               s.area as Area, s.latitude as Latitude, s.longitude as Longitude,
               s.distance_meters as DistanceMeters, d.key as DecorKey,
               d.name::text as DecorName
        from selected_spots s
        join pikmin.spot_detector sd on sd.spot_id = s.id
        join pikmin.decor_type d on d.id = sd.decor_id
        order by s.distance_meters, s.id, d.key
        """;

    public Task<IReadOnlyList<SpotModel>> SearchAreaAsync(AreaSpotFilter filter)
    {
        return QueryAsync(AreaSql, filter);
    }

    public Task<IReadOnlyList<SpotModel>> FindNearbyAsync(NearbySpotFilter filter)
    {
        return QueryAsync(NearbySql, filter);
    }

    private async Task<IReadOnlyList<SpotModel>> QueryAsync(string sql, object parameters)
    {
        var connectionString = configuration.GetConnectionString("Pikmin");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new AppException(StatusCodes.Status503ServiceUnavailable, "Spot data service is not configured.");
        }

        await using var connection = new NpgsqlConnection(connectionString);
        var rows = await connection.QueryAsync<SpotRow>(new CommandDefinition(sql, parameters, commandTimeout: 10));
        return rows.GroupBy(row => row.Id)
            .Select(group =>
            {
                var first = group.First();
                return new SpotModel(
                    first.Id, first.Name, first.Country, first.City, first.Area,
                    first.Latitude, first.Longitude,
                    group.Select(row => new DecorTypeModel(
                        row.DecorKey,
                        JsonSerializer.Deserialize<Dictionary<string, string>>(row.DecorName) ?? [])).ToArray(),
                    first.DistanceMeters);
            })
            .ToArray();
    }

    private sealed class SpotRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Area { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? DistanceMeters { get; set; }
        public string DecorKey { get; set; } = string.Empty;
        public string DecorName { get; set; } = string.Empty;
    }
}
