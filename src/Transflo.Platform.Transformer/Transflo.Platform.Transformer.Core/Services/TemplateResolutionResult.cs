using Transflo.Platform.Transformer.TransformationService.DTOs;
using ServiceModels = Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.Core.Services;

internal sealed record TemplateResolutionResult
{
    public ServiceModels.FieldMappingTemplate? Template { get; init; }
    public List<ServiceModels.FieldMapping>? Mappings { get; init; }
    public TransformationResult? EarlyResult { get; init; }

    public bool HasError => EarlyResult != null;

    public static TemplateResolutionResult Error(TransformationResult earlyResult) =>
        new() { EarlyResult = earlyResult };

    public static TemplateResolutionResult Success(
        ServiceModels.FieldMappingTemplate template,
        List<ServiceModels.FieldMapping> mappings) =>
        new() { Template = template, Mappings = mappings };
}
