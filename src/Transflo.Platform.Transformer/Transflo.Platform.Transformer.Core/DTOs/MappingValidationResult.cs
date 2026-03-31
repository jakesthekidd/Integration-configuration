using System.Text.Json;

namespace Transflo.Platform.Transformer.Core.DTOs;

/// <summary>
/// Request body for the validate endpoint.
/// When <see cref="SourceDocument"/> is provided, field values are also validated
/// against the ValidationRules of each mapping in addition to structural validation.
/// </summary>
public sealed record ValidateRequest
{
    /// <summary>
    /// The source JSON document to validate against the field mapping rules.
    /// Omit to perform structural (schema) validation only.
    /// </summary>
    public JsonElement? SourceDocument { get; init; }
}

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
