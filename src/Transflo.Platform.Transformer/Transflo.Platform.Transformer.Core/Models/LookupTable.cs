using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Transflo.Platform.Transformer.Core.Models;

[Table("lookup_tables")]
public class LookupTable : BaseEntity
{
    [Column("partner_id")]
    public Guid? PartnerId { get; set; }

    [Column("tms_system_id")]
    [Required]
    public Guid TmsSystemId { get; set; } = Guid.Empty;

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

    // Navigation property
    [ForeignKey(nameof(PartnerId))]
    public virtual Partner? Partner { get; set; }

    // Foreign key
    [ForeignKey(nameof(TmsSystemId))]
    public virtual TmsSystem? TmsSystem { get; set; }
}
