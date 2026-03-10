namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record UpdateTmsSystemRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = "1.0";
    public bool IsActive { get; set; } = true;
    public string? SampleJsonSchema { get; set; }
    public string? ConnectionConfig { get; set; }
    public string? Metadata { get; set; }
}
