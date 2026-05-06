using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record UpdateTemplateRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public TemplateStatus? Status { get; set; }
    public string? SourceSchema { get; set; }
    public string? TargetSchema { get; set; }
    public Guid? SourcePartnerId { get; set; }
    public Guid? TargetPartnerId { get; set; }
}
