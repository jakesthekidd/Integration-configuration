namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record TemplateResponse
{
    public Guid TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid TmsSystemId { get; set; }
    public Guid? CustomerId { get; set; }
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? SampleInputJson { get; set; }
    public string? Metadata { get; set; }
}
