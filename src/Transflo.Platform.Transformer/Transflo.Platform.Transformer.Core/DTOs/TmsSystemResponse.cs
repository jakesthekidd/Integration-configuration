namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record TmsSystemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Version { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? SampleJsonSchema { get; set; }
    public string? ConnectionConfig { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? Metadata { get; set; }
}
