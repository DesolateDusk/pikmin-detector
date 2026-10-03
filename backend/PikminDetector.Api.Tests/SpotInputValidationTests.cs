using System.ComponentModel.DataAnnotations;
using PikminDetector.Api.Models.Input;

namespace PikminDetector.Api.Tests;

public sealed class SpotInputValidationTests
{
    [TestCase(null, null, null, false)]
    [TestCase("", null, null, false)]
    [TestCase("TW", null, null, true)]
    [TestCase("TW", null, "信義區", false)]
    [TestCase("TW", "臺北市", "信義區", true)]
    public void AreaInput_ValidatesRequiredCountryAndConditionalCity(string? country, string? city, string? area, bool valid)
    {
        var input = new AreaSpotInput { Country = country, City = city, Area = area };

        Assert.That(IsValid(input), Is.EqualTo(valid));
        Assert.That(input.Limit, Is.EqualTo(50));
    }

    [TestCase(0, false)]
    [TestCase(1, true)]
    [TestCase(100, true)]
    [TestCase(101, false)]
    public void PaginationInput_ValidatesLimit(int limit, bool valid)
    {
        Assert.That(IsValid(new AreaSpotInput { Country = "TW", Limit = limit }), Is.EqualTo(valid));
    }

    [TestCase(null, 121d, false)]
    [TestCase(25d, null, false)]
    [TestCase(double.NaN, 121d, false)]
    [TestCase(25d, double.PositiveInfinity, false)]
    [TestCase(-90d, -180d, true)]
    [TestCase(90d, 180d, true)]
    public void NearbyInput_ValidatesRequiredFiniteCoordinates(double? latitude, double? longitude, bool valid)
    {
        var input = new NearbySpotInput { Latitude = latitude, Longitude = longitude };

        Assert.That(IsValid(input), Is.EqualTo(valid));
        Assert.That(input.RadiusMeters, Is.EqualTo(3000));
    }

    private static bool IsValid(object input) =>
        Validator.TryValidateObject(input, new ValidationContext(input), [], validateAllProperties: true);
}
