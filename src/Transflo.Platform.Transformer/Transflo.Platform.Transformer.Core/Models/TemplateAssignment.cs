using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Transflo.Platform.Transformer.Core.Models;

[Table("template_assignments")]
public class TemplateAssignment : BaseEntity
{
    [Column("template_version_id")]
    [Required]
    public Guid TemplateVersionId { get; set; } = Guid.Empty;

    [Column("source_partner_id")]
    [Required]
    public Guid SourcePartnerId { get; set; } = Guid.Empty;

    [Column("target_partner_id")]
    [Required]
    public Guid TargetPartnerId { get; set; } = Guid.Empty;

    [Column("valid_from")]
    public DateTimeOffset? ValidFrom { get; set; }

    [Column("valid_to")]
    public DateTimeOffset? ValidTo { get; set; }

    // Navigation properties
    [ForeignKey(nameof(TemplateVersionId))]
    public virtual TemplateVersion? TemplateVersion { get; set; }

    [ForeignKey(nameof(SourcePartnerId))]
    public virtual Partner? SourcePartner { get; set; }

    [ForeignKey(nameof(TargetPartnerId))]
    public virtual Partner? TargetPartner { get; set; }
}
