using PikminDetector.Api.Models.Input;
using PikminDetector.Api.Models.View;

namespace PikminDetector.Api.Services;

public interface ISpotImportService
{
    Task<SpotImportVM> ImportAsync(SpotImportInput input);
}
