namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record TemplateResponse
{
    public string TemplateId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TmsSystemId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? SampleInputJson { get; set; }
    public string? Metadata { get; set; }
}
