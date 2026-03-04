using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FieldMappingApi.Models;

[Table("tms_systems")]
public class TmsSystem
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("name")]
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("display_name")]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("version")]
    [MaxLength(50)]
    public string Version { get; set; } = "1.0";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("sample_json_schema", TypeName = "jsonb")]
    public string? SampleJsonSchema { get; set; }

    [Column("connection_config", TypeName = "jsonb")]
    public string? ConnectionConfig { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    // Navigation properties
    public virtual ICollection<FieldMappingTemplate> Templates { get; set; } = new List<FieldMappingTemplate>();
    public virtual ICollection<LookupTable> LookupTables { get; set; } = new List<LookupTable>();
}
