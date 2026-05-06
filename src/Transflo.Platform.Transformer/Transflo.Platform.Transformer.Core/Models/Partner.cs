using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Transflo.Platform.Transformer.Core.Models;

[Table("partners")]
public class Partner : BaseEntity
{
    [Column("name")]
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    // Navigation properties
    public virtual ICollection<LookupTable> LookupTables { get; set; } = new List<LookupTable>();
}
