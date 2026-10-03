namespace PikminDetector.Api.Models.View;

public sealed record SpotImportFailure(string? City, string? Area, string Reason);

public sealed record SpotImportVM(int Added, int Updated, int Removed, IReadOnlyList<SpotImportFailure> Failures);
