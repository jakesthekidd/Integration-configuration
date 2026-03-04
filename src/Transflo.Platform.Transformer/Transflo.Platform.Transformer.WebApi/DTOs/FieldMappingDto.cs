using FieldMappingApi.Models;

namespace FieldMappingApi.DTOs;

public class CreateFieldMappingRequest
{
    public string TemplateId { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public TransformationType TransformationType { get; set; } = TransformationType.Direct;
    public string? TransformationConfig { get; set; }
    public int ExecutionOrder { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
}

public class UpdateFieldMappingRequest
{
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public TransformationType TransformationType { get; set; } = TransformationType.Direct;
    public string? TransformationConfig { get; set; }
    public int ExecutionOrder { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
}

public class FieldMappingResponse
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

public class FieldMappingListResponse
{
    public List<FieldMappingResponse> Mappings { get; set; } = new();
    public int TotalCount { get; set; }
}
