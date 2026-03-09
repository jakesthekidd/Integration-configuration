using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record UpdateFieldMappingRequest
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
