using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Transflo.Platform.Transformer.Core.Models;

[Table("api_clients")]
public class ApiClient : BaseEntity
{
    [Column("name")]
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<ApiClientTemplateVersion> ApiClientTemplateVersions { get; set; } = new List<ApiClientTemplateVersion>();
}
