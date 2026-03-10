using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Transflo.Platform.Transformer.Core.Models;

[Table("template_versions")]
public class TemplateVersion : BaseEntity
{
    [Column("template_id")]
    [Required]
    [MaxLength(100)]
    public string TemplateId { get; set; } = string.Empty;

    [Column("version")]
    public int Version { get; set; } = 1;

    [Column("validation_rules", TypeName = "jsonb")]
    public string? ValidationRules { get; set; }

    // Foreign keys / Navigation
    [ForeignKey(nameof(TemplateId))]
    public virtual Template? Template { get; set; }

    public virtual ICollection<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();
    public virtual ICollection<TemplateAssignment> TemplateAssignments { get; set; } = new List<TemplateAssignment>();
}
