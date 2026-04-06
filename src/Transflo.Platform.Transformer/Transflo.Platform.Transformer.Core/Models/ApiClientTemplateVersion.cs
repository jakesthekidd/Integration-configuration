using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Transflo.Platform.Transformer.Core.Models;

[Table("api_client_template_versions")]
public class ApiClientTemplateVersion : BaseEntity
{
    [Column("api_client_id")]
    [Required]
    public Guid ApiClientId { get; set; }

    [Column("template_version_id")]
    [Required]
    public Guid TemplateVersionId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ApiClientId))]
    public virtual ApiClient? ApiClient { get; set; }

    [ForeignKey(nameof(TemplateVersionId))]
    public virtual TemplateVersion? TemplateVersion { get; set; }
}
