namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TmsSystemId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? SampleInputJson { get; set; }
    public string? Metadata { get; set; }
}
