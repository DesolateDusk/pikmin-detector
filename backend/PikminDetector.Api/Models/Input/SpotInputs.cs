using System.ComponentModel.DataAnnotations;
using PikminDetector.Api.Lib.Validation;

namespace PikminDetector.Api.Models.Input;

public sealed class AreaSpotInput : PaginationInput
{
    [Required]
    public string? Country { get; set; }

    [RequiredWhen(nameof(Area))]
    public string? City { get; set; }
    public string? Area { get; set; }
    public string? DecorTypeKey { get; set; }
}

public sealed class NearbySpotInput : PaginationInput
{
    [Required, Range(-90d, 90d)]
    public double? Latitude { get; set; }

    [Required, Range(-180d, 180d)]
    public double? Longitude { get; set; }

    [Range(1, 20000)]
    public int RadiusMeters { get; set; } = 3000;
    public string? DecorTypeKey { get; set; }
}
