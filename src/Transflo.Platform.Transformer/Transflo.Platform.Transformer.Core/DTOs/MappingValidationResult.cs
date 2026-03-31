namespace Transflo.Platform.Transformer.Core.DTOs;

public enum ValidationSeverity { Error, Warning }

public sealed record ValidationIssue
{
    public ValidationSeverity Severity { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int? MappingIndex { get; init; }
    public string? TargetPath { get; init; }
}

public sealed record MappingValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];
}
