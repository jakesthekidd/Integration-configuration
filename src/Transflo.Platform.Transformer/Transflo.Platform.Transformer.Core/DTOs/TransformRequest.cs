namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record TransformRequest
{
    public string SourceJson { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public int? Version { get; set; }
}
