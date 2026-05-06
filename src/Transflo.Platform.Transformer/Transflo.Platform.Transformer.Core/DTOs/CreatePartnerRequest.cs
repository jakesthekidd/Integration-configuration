using System.ComponentModel.DataAnnotations;

namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record CreatePartnerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}
