using PikminDetector.Api.Models.Input;
using PikminDetector.Api.Models.View;
using PikminDetector.Api.Models.DbEntity;
using PikminDetector.Api.Repositories;

namespace PikminDetector.Api.Services;

public sealed class SpotService(ISpotRepository repository) : ISpotService
{
    public async Task<IReadOnlyList<SpotVM>> SearchAreaAsync(AreaSpotInput query)
    {
        var spots = await repository.SearchAreaAsync(query);
        return spots.Select(ToVM).ToArray();
    }

    public async Task<IReadOnlyList<SpotVM>> FindNearbyAsync(NearbySpotInput query)
    {
        var spots = await repository.FindNearbyAsync(query);
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
