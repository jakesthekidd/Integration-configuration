namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record TransformOptions
{
    public string? Source { get; set; }
    public string? UserId { get; set; }
}
