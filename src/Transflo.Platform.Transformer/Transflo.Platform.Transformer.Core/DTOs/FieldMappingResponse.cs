namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record FieldMappingResponse
{
    public string Id { get; set; } = string.Empty;
    public string TemplateId { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string TransformationType { get; set; } = string.Empty;
    public string? TransformationConfig { get; set; }
    public int ExecutionOrder { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
