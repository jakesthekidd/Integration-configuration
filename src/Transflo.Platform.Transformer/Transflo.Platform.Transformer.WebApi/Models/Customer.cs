using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FieldMappingApi.Models;

[Table("customers")]
public class Customer
{
    [Key]
    [Column("id")]
    [MaxLength(100)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

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

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    // Navigation property – templates that belong to this customer
    public virtual ICollection<FieldMappingTemplate> Templates { get; set; } = new List<FieldMappingTemplate>();
}
