using PikminDetector.Api.Models.DbEntity;

namespace PikminDetector.Api.Repositories;

public interface ISpotImportRepository
{
    Task<SpotImportCounts> SynchronizeAsync(SpotImportBatch batch);
}
