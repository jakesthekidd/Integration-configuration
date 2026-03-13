namespace Transflo.Platform.Transformer.TransformationService.Models;

public class FieldMappingTemplate
{
    public Guid TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string? Description { get; set; }
}
