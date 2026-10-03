namespace PikminDetector.Api.Models.View;

public sealed record RecognitionVM(IReadOnlyList<RecognizedSeriesVM> Series);

public sealed record RecognizedSeriesVM(
    string DecorTypeKey,
    string DecorTypeName,
    IReadOnlyList<RecognizedCostumeVM> Costumes);

public sealed record RecognizedCostumeVM(
    string CostumeTypeKey,
    string CostumeTypeName,
    IReadOnlyList<RecognizedPikminVM> AvailableTypes);

public sealed record RecognizedPikminVM(string PikminType, string Status);

