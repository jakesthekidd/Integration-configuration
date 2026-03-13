using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Transflo.Platform.Transformer.Core.Models;

[Table("tms_systems")]
public class TmsSystem : BaseEntity
{
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

    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    // Navigation properties
    public virtual ICollection<LookupTable> LookupTables { get; set; } = new List<LookupTable>();
}
