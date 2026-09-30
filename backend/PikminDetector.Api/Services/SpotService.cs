using PikminDetector.Api.Models;
using PikminDetector.Api.Repositories;

namespace PikminDetector.Api.Services;

public sealed class SpotService(ISpotRepository repository) : ISpotService
{
    public async Task<IReadOnlyList<SpotVM>> SearchAreaAsync(AreaSpotInput query)
    {
        var filter = new AreaSpotFilter(
            query.Country!,
            query.City,
            query.Area,
            query.DecorTypeKey,
            query.Limit ?? 50);
        var spots = await repository.SearchAreaAsync(filter);
        return spots.Select(ToVM).ToArray();
    }

    public async Task<IReadOnlyList<SpotVM>> FindNearbyAsync(NearbySpotInput query)
    {
        var filter = new NearbySpotFilter(
            query.Latitude!.Value,
            query.Longitude!.Value,
            query.RadiusMeters ?? 3000,
            query.DecorTypeKey,
            query.Limit ?? 50);
        var spots = await repository.FindNearbyAsync(filter);
        return spots.Select(ToVM).ToArray();
    }

    private static SpotVM ToVM(SpotModel spot)
    {
        return new SpotVM(
            spot.Id, spot.Name, spot.Country, spot.City, spot.Area,
            spot.Latitude, spot.Longitude,
            spot.DecorTypes.Select(type => new DecorTypeVM(type.Key, type.Name)).ToArray(),
            spot.DistanceMeters);
    }
}
