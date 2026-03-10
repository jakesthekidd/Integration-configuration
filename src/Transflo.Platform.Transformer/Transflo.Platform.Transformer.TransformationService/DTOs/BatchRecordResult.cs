namespace Transflo.Platform.Transformer.TransformationService.DTOs;

public sealed record BatchRecordResult
{
    public int Index { get; set; }
    public bool Success { get; set; }
    public int FieldsMapped { get; set; }
    public int FieldsSkipped { get; set; }
    public long ExecutionTimeMs { get; set; }
    public Dictionary<string, object>? OutputData { get; set; }
    public List<TransformationError> Errors { get; set; } = new();
    public List<TransformationWarning> Warnings { get; set; } = new();
}
