namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record BatchTransformResult
{
    public string TemplateId { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int WarningCount { get; set; }
    public int PartialSuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public long TotalExecutionTimeMs { get; set; }
    public List<BatchRecordResult> Results { get; set; } = new();
}
