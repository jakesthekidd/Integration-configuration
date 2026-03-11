namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record TransformRequest
{
    public string SourceJson { get; set; } = string.Empty;
    public Guid TemplateId { get; set; }
    public int? Version { get; set; }
}
