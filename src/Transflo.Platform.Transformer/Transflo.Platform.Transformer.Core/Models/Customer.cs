using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Transflo.Platform.Transformer.Core.Models;

[Table("customers")]
public class Customer : BaseEntity
{
    [Column("name")]
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("code")]
    [MaxLength(50)]
    public string? Code { get; set; }

    [Column("contact_email")]
    [MaxLength(200)]
    public string? ContactEmail { get; set; }

    [Column("contact_phone")]
    [MaxLength(50)]
    public string? ContactPhone { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("notes")]
    public string? Notes { get; set; }

    // Navigation property – templates that belong to this customer

    // Navigation property – templates that belong to this customer
    public virtual ICollection<FieldMappingTemplate> Templates { get; set; } = new List<FieldMappingTemplate>();
}
