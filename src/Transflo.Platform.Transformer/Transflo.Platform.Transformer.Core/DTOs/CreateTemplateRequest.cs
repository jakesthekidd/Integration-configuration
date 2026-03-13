namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SourceSchema { get; set; }
    public string? TargetSchema { get; set; }
}
