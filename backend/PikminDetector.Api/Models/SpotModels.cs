namespace PikminDetector.Api.Models;

public sealed record AreaSpotFilter(
    string? Country,
    string? City,
    string? Area,
    string? DecorTypeKey,
    int Limit);

public sealed record NearbySpotFilter(
    double Latitude,
    double Longitude,
    int RadiusMeters,
    string? DecorTypeKey,
    int Limit);

public sealed record DecorTypeModel(
    string Key,
    IReadOnlyDictionary<string, string> Name);

public sealed record DecorTypeVM(
    string Key,
    IReadOnlyDictionary<string, string> Name);

public sealed record SpotVM(
    Guid Id,
    string Name,
    string? Country,
    string? City,
    string? Area,
    double Latitude,
    double Longitude,
    IReadOnlyList<DecorTypeVM> DecorTypes,
    double? DistanceMeters);
