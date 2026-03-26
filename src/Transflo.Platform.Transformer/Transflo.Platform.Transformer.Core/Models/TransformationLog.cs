using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Transflo.Platform.Transformer.TransformationService.DTOs;

namespace Transflo.Platform.Transformer.Core.Models;

[Table("transformation_logs")]
public class TransformationLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("template_id")]
    [Required]
    public Guid TemplateId { get; set; } = Guid.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Column("status")]
    [MaxLength(50)]
    public TransformationStatus Status { get; set; }

    [Column("input_data", TypeName = "jsonb")]
    public string? InputData { get; set; }

    [Column("output_data", TypeName = "jsonb")]
    public string? OutputData { get; set; }

    [Column("errors", TypeName = "jsonb")]
    public string? Errors { get; set; }

    [Column("execution_time_ms")]
    public long ExecutionTimeMs { get; set; }

    [Column("record_count")]
    public int RecordCount { get; set; }

    public Guid? UserId { get; set; }

    [Column("message_summary")]
    [MaxLength(500)]
    public string? MessageSummary { get; set; }

    [Column("correlation_id")]
    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    [Column("source")]
    [MaxLength(200)]
    public string? Source { get; set; }

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }
}
