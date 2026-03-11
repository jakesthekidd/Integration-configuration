namespace Transflo.Platform.Transformer.TransformationService.DTOs;

public sealed record BatchTransformResult
{
    public Guid TemplateId { get; set; }
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int WarningCount { get; set; }
    public int PartialSuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public long TotalExecutionTimeMs { get; set; }
    public List<BatchRecordResult> Results { get; set; } = new();
}
