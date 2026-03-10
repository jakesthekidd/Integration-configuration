namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record CreateTmsSystemRequest
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = "1.0";
    public string? SampleJsonSchema { get; set; }
    public string? ConnectionConfig { get; set; }
    public string? Metadata { get; set; }
}
