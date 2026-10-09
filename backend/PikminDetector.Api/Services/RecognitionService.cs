using PikminDetector.Api.Models.View;
using PikminDetector.Api.Models.DbEntity;
using PikminDetector.Api.Lib.CustomException;
using PikminDetector.Api.Repositories;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tesseract;
using System.Globalization;
using System.Text;

namespace PikminDetector.Api.Services;

public sealed class RecognitionService(IRecognitionCatalogRepository catalog) : IRecognitionService
{
    private const long MaximumImageBytes = 10_000_000;
    private const int ReferenceWidth = 1290;
    private const int BadgeX = 106;
    private const int BadgeSize = 150;
    private static readonly (double X, double Y)[] CircleDirections = Enumerable.Range(0, 32)
        .Select(index => (Math.Cos(index * Math.Tau / 32), Math.Sin(index * Math.Tau / 32)))
        .ToArray();

    public async Task<RecognitionVM> RecognizeAsync(Stream image, string contentType, long length)
    {
        if (length == 0)
            throw new CommonException(StatusCodes.Status400BadRequest, "image must not be empty.");
        if (length < 0 || length > MaximumImageBytes)
            throw new CommonException(StatusCodes.Status413PayloadTooLarge, "image must be at most 10 MB.");
        if (contentType is not ("image/png" or "image/jpeg"))
            throw new CommonException(StatusCodes.Status415UnsupportedMediaType, "Only PNG and JPEG images are accepted.");

        Image<Rgb24> screenshot;
        try
        {
            screenshot = await Image.LoadAsync<Rgb24>(image);
        }
        catch (UnknownImageFormatException)
        {
            throw new CommonException(StatusCodes.Status400BadRequest, "image is not a readable PNG or JPEG.");
        }
        catch (InvalidImageContentException)
        {
            throw new CommonException(StatusCodes.Status400BadRequest, "image is invalid.");
        }

        using (screenshot)
        {
            if (screenshot.Width < 700 || screenshot.Height < screenshot.Width * 1.8)
                throw new CommonException(StatusCodes.Status422UnprocessableEntity, "A portrait collection screenshot is required.");
            if (screenshot.Width != ReferenceWidth)
                screenshot.Mutate(operation => operation.Resize(ReferenceWidth, 0));

            var costumes = await catalog.GetAvailableCostumesAsync();
            var results = new List<RecognizedSeriesVM>();
            var headers = FindCardHeaders(screenshot);
            if (headers.Count == 0)
                return new RecognitionVM(results);

            var languageData = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
            using var mixedOcr = new TesseractEngine(
                languageData,
                "chi_tra+eng", EngineMode.LstmOnly);
            using var chineseOcr = new TesseractEngine(
                languageData,
                "chi_tra", EngineMode.LstmOnly);
            using var englishOcr = new TesseractEngine(
                languageData,
                "eng", EngineMode.LstmOnly);
            var groups = costumes.GroupBy(row => row.DecorTypeKey).ToArray();
            foreach (var headerY in headers)
            {
                var title = ReadTitle(screenshot, headerY, mixedOcr, false);
                var group = groups.FirstOrDefault(candidate =>
                    SameTitle(title, candidate.First().DecorNameEn) ||
                    SameTitle(title, candidate.First().DecorNameZh));
                if (group is null)
                {
                    title = ReadTitle(screenshot, headerY, chineseOcr, true);
                    group = groups.FirstOrDefault(candidate =>
                        SameTitle(title, candidate.First().DecorNameEn) ||
                        SameTitle(title, candidate.First().DecorNameZh));
                }
                if (group is null)
                {
                    title = ReadTitle(screenshot, headerY, englishOcr, true);
                    group = groups.FirstOrDefault(candidate =>
                        SameTitle(title, candidate.First().DecorNameEn) ||
                        SameTitle(title, candidate.First().DecorNameZh));
                }
                if (group is null)
                    continue;
                var rows = group.OrderBy(row => row.DisplayOrder).ToArray();
                var top = headerY - 40;
                var bottom = FindCardBottom(screenshot, top);
                if (bottom is null)
                    continue;
                var allSlots = Layout(rows);
                var visibleCostumes = rows
                    .Where(costume => allSlots
                        .Where(slot => slot.CostumeKey == costume.CostumeTypeKey)
                        .All(slot => bottom.Value >= top + 420 + slot.Row * 320 + 125))
                    .Select(costume => costume.CostumeTypeKey)
                    .ToHashSet(StringComparer.Ordinal);
                var slots = allSlots.Where(slot => visibleCostumes.Contains(slot.CostumeKey)).ToArray();
                if (slots.Length == 0)
                    continue;

                var recognizedCostumes = rows
                    .Where(row => visibleCostumes.Contains(row.CostumeTypeKey))
                    .Select(row => new RecognizedCostumeVM(
                        row.CostumeTypeKey,
                        DisplayName(row.CostumeNameZh, row.CostumeNameEn, row.CostumeTypeKey),
                        slots.Where(slot => slot.CostumeKey == row.CostumeTypeKey)
                            .Select(slot => new RecognizedPikminVM(
                                slot.PikminType,
                                IsColored(screenshot, slot.X, top + 420 + slot.Row * 320)
                                    ? "collected" : "missing"))
                            .ToArray()))
                    .ToArray();
                results.Add(new RecognizedSeriesVM(
                    group.Key,
                    DisplayName(rows[0].DecorNameZh, rows[0].DecorNameEn, group.Key),
                    recognizedCostumes));
            }
            return new RecognitionVM(results);
        }
    }

    private static string DisplayName(string zh, string en, string key) =>
        !string.IsNullOrWhiteSpace(zh) ? zh : !string.IsNullOrWhiteSpace(en) ? en : key;

    private static string ReadTitle(Image<Rgb24> screenshot, int headerY, TesseractEngine ocr, bool enlarge)
    {
        if (headerY + 125 >= screenshot.Height)
            return string.Empty;
        using var title = screenshot.Clone(operation =>
        {
            operation.Crop(new SixLabors.ImageSharp.Rectangle(270, headerY + 15, 600, 110));
            if (enlarge)
                operation.Resize(1800, 330);
        });
        using var png = new MemoryStream();
        title.SaveAsPng(png);
        using var pix = Pix.LoadFromMemory(png.ToArray());
        using var page = ocr.Process(pix, PageSegMode.SingleLine);
        return page.GetText();
    }

    private static bool SameTitle(string recognized, string expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
            return false;
        return NormalizeTitle(recognized) == NormalizeTitle(expected);
    }

    private static string NormalizeTitle(string text)
    {
        var builder = new StringBuilder();
        foreach (var character in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(character))
                builder.Append(char.ToLowerInvariant(character));
        }
        return builder.ToString();
    }

    private static IReadOnlyList<int> FindCardHeaders(Image<Rgb24> image)
    {
        var matches = new List<(int Y, double Contrast)>();
        var centerX = BadgeX + BadgeSize / 2;
        for (var y = 300; y < image.Height - BadgeSize - 8; y += 2)
        {
            var centerY = y + BadgeSize / 2;
            var edges = 0;
            double contrast = 0;
            // Sample the badge border and its surrounding card, leaving the category artwork out.
            foreach (var direction in CircleDirections)
            {
                var outside = image[centerX + (int)Math.Round(direction.X * 82),
                    centerY + (int)Math.Round(direction.Y * 82)];
                if (outside.R < 230 || outside.G < 230 || outside.B < 220 ||
                    Math.Max(outside.R, Math.Max(outside.G, outside.B)) -
                    Math.Min(outside.R, Math.Min(outside.G, outside.B)) > 20)
                    continue;

                var borderContrast = 0;
                for (var radius = 67; radius <= 74; radius++)
                {
                    var border = image[centerX + (int)Math.Round(direction.X * radius),
                        centerY + (int)Math.Round(direction.Y * radius)];
                    borderContrast = Math.Max(borderContrast,
                        (outside.R + outside.G + outside.B - border.R - border.G - border.B) / 3);
                }
                if (borderContrast < 25)
                    continue;
                edges++;
                contrast += borderContrast;
            }
            // Allow highlights and shadows to obscure up to a quarter of the circular border.
            if (edges >= CircleDirections.Length * 3 / 4)
                matches.Add((y, contrast));
        }

        var found = new List<int>();
        foreach (var match in matches.OrderByDescending(match => match.Contrast))
        {
            if (found.All(y => Math.Abs(y - match.Y) > BadgeSize))
                found.Add(match.Y);
        }
        return found.Order().ToArray();
    }

    private static int? FindCardBottom(Image<Rgb24> image, int top)
    {
        var nonWhiteRun = 0;
        for (var y = Math.Max(top + 180, 0); y < image.Height; y += 3)
        {
            var white = 0;
            foreach (var x in new[] { 105, 300, 650, 1000, 1185 })
            {
                var pixel = image[x, y];
                if (pixel.R > 242 && pixel.G > 242 && pixel.B > 235)
                    white++;
            }
            nonWhiteRun = white >= 2 ? 0 : nonWhiteRun + 1;
            if (nonWhiteRun >= 10)
                return y - 27;
        }
        return null;
    }

    private static IReadOnlyList<(string CostumeKey, string PikminType, int X, int Row)> Layout(
        IReadOnlyList<CostumeCatalogRow> costumes)
    {
        var slots = new List<(string, string, int, int)>();
        if (costumes.Count > 1 && costumes.All(costume => costume.AvailableTypes.Length == 1))
        {
            for (var index = 0; index < costumes.Count; index++)
            {
                var packedRow = index / 3;
                var columns = Math.Min(3, costumes.Count - packedRow * 3);
                var col = index % 3;
                var x = columns switch
                {
                    3 => 355 + col * 290,
                    2 => 500 + col * 290,
                    _ => 645
                };
                slots.Add((costumes[index].CostumeTypeKey,
                    costumes[index].AvailableTypes[0], x, packedRow));
            }
            return slots;
        }

        var row = 0;
        foreach (var costume in costumes)
        {
            var types = costume.AvailableTypes;
            for (var index = 0; index < types.Length;)
            {
                var columns = types.Length - index == 7 ? 3 : Math.Min(4, types.Length - index);
                for (var col = 0; col < columns; col++)
                {
                    var x = columns switch
                    {
                        4 => 215 + col * 290,
                        3 => 355 + col * 290,
                        2 => 500 + col * 290,
                        _ => 645
                    };
                    slots.Add((costume.CostumeTypeKey, types[index++], x, row));
                }
                row++;
            }
        }
        return slots;
    }

    private static bool IsColored(Image<Rgb24> image, int centerX, int centerY)
    {
        var saturated = 0;
        for (var y = Math.Max(0, centerY - 110); y < Math.Min(image.Height, centerY + 110); y += 3)
        {
            for (var x = centerX - 100; x < centerX + 100; x += 3)
            {
                var pixel = image[x, y];
                var max = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
                var min = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
                if (max - min > 35 && min < 225)
                    saturated++;
            }
        }
        return saturated >= 60;
    }
}
