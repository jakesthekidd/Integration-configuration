using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Transflo.Platform.Transformer.TransformationService.DTOs;

namespace Transflo.Platform.Transformer.Core.Models;

[Table("transformation_logs")]
public class TransformationLog
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("template_id")]
    [Required]
    [MaxLength(100)]
    public string TemplateId { get; set; } = string.Empty;

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

    [Column("user_id")]
    [MaxLength(100)]
    public string? UserId { get; set; }

    [Column("source")]
    [MaxLength(200)]
    public string? Source { get; set; }

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }
}
