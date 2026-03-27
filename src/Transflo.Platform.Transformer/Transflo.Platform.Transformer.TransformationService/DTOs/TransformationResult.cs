namespace Transflo.Platform.Transformer.TransformationService.DTOs;

public sealed record TransformationResult
{
    public bool Success { get; set; }
    public Dictionary<string, object>? TransformedData { get; set; }
    public Dictionary<string, object>? OutputData { get; set; }
    public string? OutputJson { get; set; }
    public int FieldsMapped { get; set; }
    public int FieldsSkipped { get; set; }
    public List<TransformationError> Errors { get; set; } = new();
    public List<TransformationWarning> Warnings { get; set; } = new();
    public long ExecutionTimeMs { get; set; }
    public string? MessageSummary { get; set; }
}
