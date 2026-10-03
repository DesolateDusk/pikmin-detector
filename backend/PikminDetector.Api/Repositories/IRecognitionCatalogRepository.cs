using PikminDetector.Api.Models.DbEntity;

namespace PikminDetector.Api.Repositories;

public interface IRecognitionCatalogRepository
{
    Task<IReadOnlyList<CostumeCatalogRow>> GetAvailableCostumesAsync();
}
