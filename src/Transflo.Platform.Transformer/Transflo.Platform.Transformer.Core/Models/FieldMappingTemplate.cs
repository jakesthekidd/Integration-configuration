using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Transflo.Platform.Transformer.Core.Models;

public enum TemplateStatus
{
    Draft,
    Published,
    Archived
}

[Table("field_mapping_templates")]
public class FieldMappingTemplate
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("template_id")]
    [Required]
    [MaxLength(100)]
    public string TemplateId { get; set; } = Guid.NewGuid().ToString();

    [Column("version")]
    public int Version { get; set; } = 1;

    [Column("tms_system_id")]
    [Required]
    [MaxLength(100)]
    public string TmsSystemId { get; set; } = string.Empty;

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

    [Column("created_by")]
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

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
    [MaxLength(100)]
    public string? CustomerId { get; set; }

    // Foreign keys / navigation
    [ForeignKey(nameof(TmsSystemId))]
    public virtual TmsSystem? TmsSystem { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    // Navigation properties
    public virtual ICollection<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();
}
