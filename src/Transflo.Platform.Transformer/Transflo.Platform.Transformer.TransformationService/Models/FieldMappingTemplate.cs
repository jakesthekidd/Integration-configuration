namespace Transflo.Platform.Transformer.TransformationService.Models;

public class FieldMappingTemplate
{
    public string TemplateId { get; set; } = string.Empty;
    public string TmsSystemId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string? Description { get; set; }
    public string? SampleInputJson { get; set; }
    public string? Metadata { get; set; }
}
