using Microsoft.AspNetCore.Http;
using PikminDetector.Api.Common.Errors;
using PikminDetector.Api.Models;

namespace PikminDetector.Api.Services;

public static class TreelazyCatalog
{
    public static readonly IReadOnlyDictionary<string, string> TaiwanCities = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["臺北市"] = "taipei", ["新北市"] = "new-taipei", ["桃園市"] = "taoyuan",
        ["臺中市"] = "taichung", ["臺南市"] = "tainan", ["高雄市"] = "kaohsiung",
        ["基隆市"] = "keelung", ["新竹市"] = "hsinchu", ["嘉義市"] = "chiayi",
        ["新竹縣"] = "hsinchu-county", ["苗栗縣"] = "miaoli", ["彰化縣"] = "changhua",
        ["南投縣"] = "nantou", ["雲林縣"] = "yunlin", ["嘉義縣"] = "chiayi-county",
        ["屏東縣"] = "pingtung", ["宜蘭縣"] = "yilan", ["花蓮縣"] = "hualien",
        ["臺東縣"] = "taitung", ["澎湖縣"] = "penghu", ["金門縣"] = "kinmen",
        ["連江縣"] = "lienchiang"
    };

    public static readonly IReadOnlyDictionary<string, (string Slug, bool HasCities)> Countries =
        new Dictionary<string, (string, bool)>(StringComparer.Ordinal)
        {
            ["JP"] = ("japan", true), ["SG"] = ("singapore", true),
            ["HK"] = ("hong-kong", true), ["KR"] = ("korea", true),
            ["MO"] = ("macau", false), ["US"] = ("usa", true),
            ["CH"] = ("switzerland", true), ["LU"] = ("luxembourg", true),
            ["AU"] = ("australia", true), ["CA"] = ("canada", true),
            ["IL"] = ("israel", true), ["GB"] = ("united-kingdom", true),
            ["IE"] = ("ireland", true), ["SE"] = ("sweden", true),
            ["IN"] = ("india", true), ["FR"] = ("france", false),
            ["DE"] = ("germany", true), ["TR"] = ("turkiye", false),
            ["NL"] = ("netherlands", false), ["MX"] = ("mexico", false),
            ["AT"] = ("austria", false), ["ES"] = ("spain", false),
            ["MY"] = ("malaysia", true)
        };

    public static SpotImportScope Validate(SpotImportRequest request)
    {
        var country = request.Country;
        var city = request.City;
        var area = request.Area;
        if (country is null || (country != "TW" && !Countries.ContainsKey(country)))
            throw new AppException(StatusCodes.Status400BadRequest, "Unsupported country code.");
        if (city == "" || area == "" || (area is not null && city is null))
            throw new AppException(StatusCodes.Status400BadRequest, "A valid city is required when area is provided.");
        if (country == "TW")
        {
            if (city is not null && !TaiwanCities.ContainsKey(city))
                throw new AppException(StatusCodes.Status400BadRequest, "Unknown Taiwan city.");
        }
        else if (area is not null || (city is not null && !Countries[country].HasCities))
            throw new AppException(StatusCodes.Status400BadRequest, "This country does not support the requested city or area scope.");
        return new SpotImportScope(country, city, area);
    }
}
