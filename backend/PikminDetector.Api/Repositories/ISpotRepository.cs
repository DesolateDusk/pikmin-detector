using PikminDetector.Api.Models;

namespace PikminDetector.Api.Repositories;

public interface ISpotRepository
{
    Task<IReadOnlyList<SpotModel>> SearchAreaAsync(AreaSpotFilter filter);
    Task<IReadOnlyList<SpotModel>> FindNearbyAsync(NearbySpotFilter filter);
}
