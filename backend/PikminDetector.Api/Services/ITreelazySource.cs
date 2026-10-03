using PikminDetector.Api.Models.DbEntity;

namespace PikminDetector.Api.Services;

public interface ITreelazySource
{
    Task<IReadOnlyList<string>> GetAreasAsync(string city);
    Task<SpotImportBatch> GetTaiwanAreaAsync(string city, string area);
    Task<SpotImportBatch> GetCountryAsync(SpotImportScope scope);
}
