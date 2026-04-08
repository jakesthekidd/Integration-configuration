using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ServiceModels = Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.Core.Models;

[Table("field_mappings")]
public class FieldMapping : BaseEntity
{
    [Column("template_version_id")]
    [Required]
    public Guid TemplateVersionId { get; set; }

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

    [Column("is_required")]
    public bool IsRequired { get; set; }

    [Column("default_value")]
    public string? DefaultValue { get; set; }

    [Column("validation_rules", TypeName = "jsonb")]
    public string? ValidationRules { get; set; }

    // Navigation properties
    [ForeignKey(nameof(TemplateVersionId))]
    public virtual TemplateVersion? TemplateVersion { get; set; }
}
