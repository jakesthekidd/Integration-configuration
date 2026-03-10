namespace Transflo.Platform.Transformer.TransformationService.Models;

public class FieldMappingTemplate
{
    public Guid TemplateId { get; set; }
    public Guid TmsSystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string? Description { get; set; }
    public string? SampleInputJson { get; set; }
    public string? Metadata { get; set; }
}
