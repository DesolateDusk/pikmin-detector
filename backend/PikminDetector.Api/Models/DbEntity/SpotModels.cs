namespace PikminDetector.Api.Models.DbEntity;

public sealed record DecorTypeModel(
    string Key,
    IReadOnlyDictionary<string, string> Name);
