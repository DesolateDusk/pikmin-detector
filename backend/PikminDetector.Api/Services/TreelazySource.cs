using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using PikminDetector.Api.Models;

namespace PikminDetector.Api.Services;

public sealed class TreelazySource(HttpClient http)
{
    private static readonly IReadOnlyDictionary<string, string> DecorKeys =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["airport"] = "airport", ["electronics"] = "appliances_store",
            ["art_gallery"] = "art_gallery", ["bakery"] = "bakery",
            ["beach"] = "beach", ["bridge"] = "bridge", ["bus_stop"] = "bus_stop",
            ["burger"] = "burger_place", ["cafe"] = "cafe",
            ["clothing"] = "clothes_store", ["convenience"] = "corner_store",
            ["curry"] = "curry_restaurant", ["hardware"] = "diy_store",
            ["forest"] = "forest", ["hair_salon"] = "hair_salon", ["hotel"] = "hotel",
            ["italian"] = "italian_restaurant", ["korean"] = "korean_restaurant",
            ["laundromat"] = "laundromats_and_dry_cleaners",
            ["library"] = "library_and_bookstore", ["cosmetics"] = "makeup_store",
            ["mexican"] = "mexican_restaurant", ["cinema"] = "movie_theater",
            ["hill"] = "mountain", ["park"] = "park", ["pharmacy"] = "pharmacy",
            ["post_office"] = "post_office", ["ramen"] = "ramen_restaurant",
            ["restaurant"] = "restaurant", ["roadside"] = "roadside",
            ["shrine"] = "shrine_and_temple", ["station"] = "station",
            ["stationery"] = "stationery_store", ["stadium"] = "stadium",
            ["supermarket"] = "supermarket", ["sushi"] = "sushi_restaurant",
            ["sweetshop"] = "sweetshop", ["theme_park"] = "theme_park",
            ["university"] = "university_and_college", ["water"] = "waterside",
            ["zoo"] = "zoo"
        };

    public async Task<IReadOnlyList<string>> GetAreasAsync(string city)
    {
        var slug = TreelazyCatalog.TaiwanCities[city];
        var html = await http.GetStringAsync($"https://treelazy.com/pikmin/{slug}");
        var pattern = $"<a\\s+href=\"/pikmin/{Regex.Escape(slug)}/[^\"]+\">(?<area>[^<]+)</a>";
        var areas = Regex.Matches(html, pattern, RegexOptions.IgnoreCase)
            .Select(match => WebUtility.HtmlDecode(match.Groups["area"].Value.Trim()))
            .Distinct(StringComparer.Ordinal).ToArray();
        if (areas.Length == 0)
            throw new InvalidDataException($"No areas found for {city}.");
        return areas;
    }

    public async Task<SpotImportBatch> GetTaiwanAreaAsync(string city, string area)
    {
        var url = $"https://treelazy.com/pikmin/data/{Uri.EscapeDataString(city)}/{Uri.EscapeDataString(area)}.json";
        return await ReadAsync(url, new SpotImportScope("TW", city, area));
    }

    public async Task<SpotImportBatch> GetCountryAsync(SpotImportScope scope)
    {
        var country = TreelazyCatalog.Countries[scope.Country];
        var url = $"https://treelazy.com/pikmin/data/{country.Slug}.json";
        return await ReadAsync(url, scope);
    }

    private async Task<SpotImportBatch> ReadAsync(string url, SpotImportScope scope)
    {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            throw new InvalidDataException("Source returned an empty or invalid spot list.");

        var spots = new Dictionary<(double, double), ImportedSpot>();
        var sourceCount = 0;
        foreach (var item in root.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("Source spot must be an object.");
            var sourceCity = item.TryGetProperty("city", out var cityValue) &&
                             cityValue.ValueKind == JsonValueKind.String
                ? cityValue.GetString()
                : null;
            if (scope.Country != "TW" && scope.City is not null &&
                !string.Equals(sourceCity, scope.City, StringComparison.Ordinal))
                continue;
            sourceCount++;
            var status = item.GetProperty("status").GetString();
            if (status is not ("pure" or "mixed"))
                throw new InvalidDataException($"Unknown source status: {status ?? "null"}.");
            if (status != "pure") continue;

            if (
                !item.TryGetProperty("decorTypes", out var types) || types.ValueKind != JsonValueKind.Array ||
                types.GetArrayLength() != 1)
                throw new InvalidDataException("A pure source spot must have exactly one decor type.");

            var keys = new List<string>();
            foreach (var type in types.EnumerateArray())
            {
                var sourceKey = type.GetString();
                if (sourceKey is null || !DecorKeys.TryGetValue(sourceKey, out var key))
                    throw new InvalidDataException($"Unknown source decor key: {sourceKey ?? "null"}.");
                if (!keys.Contains(key, StringComparer.Ordinal)) keys.Add(key);
            }


            var id = item.GetProperty("id").GetString();
            var name = item.GetProperty("name").GetString();
            var lat = item.GetProperty("lat").GetDouble();
            var lon = item.GetProperty("lon").GetDouble();
            if (string.IsNullOrWhiteSpace(id) || name is null || !double.IsFinite(lat) ||
                !double.IsFinite(lon) || lat is < -90 or > 90 || lon is < -180 or > 180)
                throw new InvalidDataException("Source spot has invalid id, name, or coordinates.");
            var spot = new ImportedSpot(id, name, scope.Country,
                string.IsNullOrWhiteSpace(sourceCity) ? scope.City : sourceCity,
                scope.Country == "TW" ? scope.Area : null,
                lat, lon, keys);
            if (!spots.TryAdd((lat, lon), spot))
                throw new InvalidDataException($"Source contains duplicate coordinates: {lat}, {lon}.");
        }
        if (sourceCount == 0)
            throw new InvalidDataException("Source has no spots for the requested city.");
        return new SpotImportBatch(scope, sourceCount, spots.Values.ToArray());
    }
}
