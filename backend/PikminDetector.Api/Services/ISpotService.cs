using PikminDetector.Api.Models.Input;
using PikminDetector.Api.Models.View;

namespace PikminDetector.Api.Services;

public interface ISpotService
{
    Task<IReadOnlyList<SpotVM>> SearchAreaAsync(AreaSpotInput query);
    Task<IReadOnlyList<SpotVM>> FindNearbyAsync(NearbySpotInput query);
}
