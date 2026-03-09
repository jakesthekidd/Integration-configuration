namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record TransformationWarning
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SourcePath { get; set; }
    public string? TargetPath { get; set; }
}
