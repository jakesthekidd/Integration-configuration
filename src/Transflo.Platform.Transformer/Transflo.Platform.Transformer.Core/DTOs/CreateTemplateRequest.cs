namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid TmsSystemId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? SampleInputJson { get; set; }
    public string? Metadata { get; set; }
}
