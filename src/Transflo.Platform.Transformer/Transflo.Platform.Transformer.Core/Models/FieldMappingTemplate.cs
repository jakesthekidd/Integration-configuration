using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Transflo.Platform.Transformer.Core.Models;


[Table("field_mapping_templates")]
public class FieldMappingTemplate : BaseEntity
{
    [Column("template_id")]
    [Required]
    public Guid TemplateId { get; set; } = Guid.NewGuid();

    [Column("version")]
    public int Version { get; set; } = 1;

    [Column("tms_system_id")]
    [Required]
    public Guid TmsSystemId { get; set; } = Guid.Empty;

    [Column("name")]
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public TemplateStatus Status { get; set; } = TemplateStatus.Draft;

    [Column("source_schema", TypeName = "jsonb")]
    public string? SourceSchema { get; set; }

    [Column("target_schema", TypeName = "jsonb")]
    public string? TargetSchema { get; set; }

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }

    [Column("published_by")]
    [MaxLength(100)]
    public string? PublishedBy { get; set; }

    [Column("sample_input_json", TypeName = "jsonb")]
    public string? SampleInputJson { get; set; }

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    [Column("customer_id")]
    public Guid? CustomerId { get; set; }

    // Foreign keys / navigation
    [ForeignKey(nameof(TmsSystemId))]
    public virtual TmsSystem? TmsSystem { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    // Navigation properties
    public virtual ICollection<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();
}
