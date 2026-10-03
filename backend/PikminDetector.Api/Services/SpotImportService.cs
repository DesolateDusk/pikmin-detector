using PikminDetector.Api.Models.Input;
using PikminDetector.Api.Models.View;
using PikminDetector.Api.Repositories;

namespace PikminDetector.Api.Services;

public sealed class SpotImportService(ITreelazySource source, ISpotImportRepository repository, ILogger<SpotImportService> logger) : ISpotImportService
{
    public async Task<SpotImportVM> ImportAsync(SpotImportInput request)
    {
        var scope = TreelazyCatalog.Validate(request);
        var added = 0;
        var updated = 0;
        var removed = 0;
        var failures = new List<SpotImportFailure>();

        if (scope.Country != "TW")
        {
            var batch = await source.GetCountryAsync(scope);
            var counts = await repository.SynchronizeAsync(batch);
            return new SpotImportVM(counts.Added, counts.Updated, counts.Removed, []);
        }

        var cities = scope.City is null ? TreelazyCatalog.TaiwanCities.Keys : [scope.City];
        foreach (var city in cities)
        {
            IReadOnlyList<string> areas;
            try
            {
                areas = await source.GetAreasAsync(city);
                if (scope.Area is not null)
                {
                    if (!areas.Contains(scope.Area, StringComparer.Ordinal))
                    {
                        failures.Add(new SpotImportFailure(city, scope.Area, "Area was not found in the source."));
                        continue;
                    }
                    areas = [scope.Area];
                }
            }
            catch (Exception error) when (error is HttpRequestException or InvalidDataException)
            {
                logger.LogWarning(error, "Could not list areas for {City}", city);
                failures.Add(new SpotImportFailure(city, scope.Area, error.Message));
                continue;
            }

            foreach (var area in areas)
            {
                try
                {
                    var batch = await source.GetTaiwanAreaAsync(city, area);
                    var counts = await repository.SynchronizeAsync(batch);
                    added += counts.Added;
                    updated += counts.Updated;
                    removed += counts.Removed;
                }
                catch (Exception error) when (error is HttpRequestException or InvalidDataException)
                {
                    logger.LogWarning(error, "Spot import failed for {City}/{Area}", city, area);
                    failures.Add(new SpotImportFailure(city, area, error.Message));
                }
            }
        }

        return new SpotImportVM(added, updated, removed, failures);
    }
}
