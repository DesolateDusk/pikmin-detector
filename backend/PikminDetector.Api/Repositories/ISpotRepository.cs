using PikminDetector.Api.Models.Input;
using PikminDetector.Api.Models.DbEntity;

namespace PikminDetector.Api.Repositories;

public interface ISpotRepository
{
    Task<IReadOnlyList<SpotModel>> SearchAreaAsync(AreaSpotInput filter);
    Task<IReadOnlyList<SpotModel>> FindNearbyAsync(NearbySpotInput filter);
}
