using System.ComponentModel.DataAnnotations;

namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record CreateApiClientRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
