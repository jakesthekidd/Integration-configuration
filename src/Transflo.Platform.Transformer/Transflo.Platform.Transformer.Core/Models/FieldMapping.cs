using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Transflo.Platform.Transformer.Core.Models;

public enum TransformationType
{
    Direct,
    Concat,
    Lookup,
    Conditional,
    ArrayMap,
    ArrayFlatten,
    DateFormat,
    Math,
    Substring,
    Constant,
    Template
}

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
    public TransformationType TransformationType { get; set; } = TransformationType.Direct;

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

    // Foreign key (Note: using string reference to TemplateId, not int Id)
    [NotMapped]
    public virtual FieldMappingTemplate? Template { get; set; }
}
