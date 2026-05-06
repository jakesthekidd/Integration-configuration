using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Transflo.Platform.Transformer.Core.Models;

[Table("templates")]
public class Template : BaseEntity
{
    [Column("name")]
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public TemplateStatus Status { get; set; } = TemplateStatus.Active;

    [Column("source_schema", TypeName = "jsonb")]
    public string? SourceSchema { get; set; }

    [Column("target_schema", TypeName = "jsonb")]
    public string? TargetSchema { get; set; }

    [Column("source_partner_id")]
    public Guid? SourcePartnerId { get; set; }

    [Column("target_partner_id")]
    public Guid? TargetPartnerId { get; set; }

    // Navigation properties
    public virtual ICollection<TemplateVersion> TemplateVersions { get; set; } = new List<TemplateVersion>();

    [ForeignKey(nameof(SourcePartnerId))]
    public virtual Partner? SourcePartner { get; set; }

    [ForeignKey(nameof(TargetPartnerId))]
    public virtual Partner? TargetPartner { get; set; }
}
