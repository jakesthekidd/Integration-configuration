namespace Transflo.Platform.Transformer.TransformationService.DTOs;

public sealed record MappingResult
{
    public int FieldsMapped { get; init; }
    public int FieldsSkipped { get; init; }
    public List<TransformationWarning> Warnings { get; init; } = new();
    public List<TransformationError> Errors { get; init; } = new();
}
