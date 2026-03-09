namespace Transflo.Platform.Transformer.TransformationService.DTOs;

public sealed record TransformationError
{
    public string ErrorCode { get; set; } = string.Empty;
    public string? FieldPath { get; set; }
    public string? SourcePath { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
}
