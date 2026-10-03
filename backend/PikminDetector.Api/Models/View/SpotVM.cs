namespace PikminDetector.Api.Models.View;

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
