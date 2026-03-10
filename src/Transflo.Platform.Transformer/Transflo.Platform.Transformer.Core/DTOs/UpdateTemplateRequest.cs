using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record UpdateTemplateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public TemplateStatus? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public string? SampleInputJson { get; set; }
    public string? Metadata { get; set; }
}
