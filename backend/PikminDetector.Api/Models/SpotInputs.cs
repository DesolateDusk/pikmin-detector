using System.ComponentModel.DataAnnotations;

namespace PikminDetector.Api.Models;

public sealed class AreaSpotInput : IValidatableObject
{
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Area { get; set; }
    public string? DecorTypeKey { get; set; }
    public int? Limit { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Country is null)
            yield return new ValidationResult("country is required.", [nameof(Country)]);
        if (Area is not null && City is null)
            yield return new ValidationResult("city is required when area is provided.", [nameof(City)]);
        if (Limit is < 1 or > 100)
            yield return new ValidationResult("limit must be between 1 and 100.", [nameof(Limit)]);
    }
}

public sealed class NearbySpotInput : IValidatableObject
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? RadiusMeters { get; set; }
    public string? DecorTypeKey { get; set; }
    public int? Limit { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Latitude is not { } latitude || !double.IsFinite(latitude) || latitude is < -90 or > 90)
            yield return new ValidationResult("latitude must be between -90 and 90.", [nameof(Latitude)]);
        if (Longitude is not { } longitude || !double.IsFinite(longitude) || longitude is < -180 or > 180)
            yield return new ValidationResult("longitude must be between -180 and 180.", [nameof(Longitude)]);
        if (RadiusMeters is < 1 or > 20000)
            yield return new ValidationResult("radiusMeters must be between 1 and 20000.", [nameof(RadiusMeters)]);
        if (Limit is < 1 or > 100)
            yield return new ValidationResult("limit must be between 1 and 100.", [nameof(Limit)]);
    }
}
