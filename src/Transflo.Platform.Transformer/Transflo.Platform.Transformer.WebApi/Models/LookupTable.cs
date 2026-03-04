using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FieldMappingApi.Models;

[Table("lookup_tables")]
public class LookupTable
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("tms_system_id")]
    [Required]
    [MaxLength(100)]
    public string TmsSystemId { get; set; } = string.Empty;

    [Column("field_name")]
    [Required]
    [MaxLength(200)]
    public string FieldName { get; set; } = string.Empty;

    [Column("name")]
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("mappings", TypeName = "jsonb")]
    public string? Mappings { get; set; }

    [Column("default_value")]
    [MaxLength(500)]
    public string? DefaultValue { get; set; }

    [Column("is_case_sensitive")]
    public bool IsCaseSensitive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    // Foreign key
    [ForeignKey(nameof(TmsSystemId))]
    public virtual TmsSystem? TmsSystem { get; set; }
}
