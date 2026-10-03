namespace PikminDetector.Api.Models.DbEntity;

public sealed record SpotImportScope(string Country, string? City, string? Area);

public sealed record ImportedSpot(
    string SourceId, string Name, string Country, string? City, string? Area,
    double Latitude, double Longitude, IReadOnlyList<string> DecorKeys);

public sealed record SpotImportBatch(SpotImportScope Scope, int SourceCount, IReadOnlyList<ImportedSpot> Spots);

public sealed record SpotImportCounts(int Added, int Updated, int Removed);

