namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record TemplateVersionResponse
{
    /// <summary>Row PK of the TemplateVersion record.</summary>
    public Guid Id { get; set; }

    public Guid TemplateId { get; set; }
    public int Version { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ValidationRules { get; set; }
    public string? Metadata { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
