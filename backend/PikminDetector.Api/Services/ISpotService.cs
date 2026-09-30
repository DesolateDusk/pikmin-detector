using PikminDetector.Api.Models;

namespace PikminDetector.Api.Services;

public interface ISpotService
{
    Task<IReadOnlyList<SpotVM>> SearchAreaAsync(AreaSpotInput query);
    Task<IReadOnlyList<SpotVM>> FindNearbyAsync(NearbySpotInput query);
}
