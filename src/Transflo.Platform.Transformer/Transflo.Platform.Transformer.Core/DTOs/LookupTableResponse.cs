namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record LookupTableResponse
{
    public string Id { get; set; } = string.Empty;
    public string TmsSystemId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Mappings { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsCaseSensitive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
