using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceModels = Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.Core.Models;

[Table("field_mappings")]
public class FieldMapping
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("template_id")]
    [Required]
    [MaxLength(100)]
    public string TemplateId { get; set; } = string.Empty;

    [Column("source_path")]
    [Required]
    [MaxLength(500)]
    public string SourcePath { get; set; } = string.Empty;

    [Column("target_path")]
    [Required]
    [MaxLength(500)]
    public string TargetPath { get; set; } = string.Empty;

    [Column("transformation_type")]
    [MaxLength(50)]
    public ServiceModels.TransformationType TransformationType { get; set; } = ServiceModels.TransformationType.Direct;

    [Column("transformation_config", TypeName = "jsonb")]
    public string? TransformationConfig { get; set; }

    [Column("execution_order")]
    public int ExecutionOrder { get; set; }

    [Column("is_required")]
    public bool IsRequired { get; set; }

    [Column("default_value")]
    public string? DefaultValue { get; set; }

    [Column("validation_rules", TypeName = "jsonb")]
    public string? ValidationRules { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public virtual FieldMappingTemplate? Template { get; set; }
}
