namespace PikminDetector.Api.Models.DbEntity;

public sealed record SpotModel(
    Guid Id,
    string Name,
    string? Country,
    string? City,
    string? Area,
    double Latitude,
    double Longitude,
    IReadOnlyList<DecorTypeModel> DecorTypes,
    double? DistanceMeters);
