namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record CreateLookupTableRequest
{
    public string TmsSystemId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Mappings { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsCaseSensitive { get; set; } = true;
}
