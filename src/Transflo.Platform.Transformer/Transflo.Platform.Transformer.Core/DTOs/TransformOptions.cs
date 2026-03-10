namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record TransformOptions
{
    public string? Source { get; set; }
    public Guid? UserId { get; set; }
}
