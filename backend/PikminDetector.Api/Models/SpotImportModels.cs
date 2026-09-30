namespace PikminDetector.Api.Models;

public sealed record SpotImportRequest(string? Country, string? City, string? Area);

public sealed record SpotImportScope(string Country, string? City, string? Area);

public sealed record ImportedSpot(
    string SourceId, string Name, string Country, string? City, string? Area,
    double Latitude, double Longitude, IReadOnlyList<string> DecorKeys);

public sealed record SpotImportBatch(SpotImportScope Scope, int SourceCount, IReadOnlyList<ImportedSpot> Spots);

public sealed record SpotImportCounts(int Added, int Updated, int Removed);

public sealed record SpotImportFailure(string? City, string? Area, string Reason);

public sealed record SpotImportVM(int Added, int Updated, int Removed, IReadOnlyList<SpotImportFailure> Failures);
