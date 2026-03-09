namespace Transflo.Platform.Transformer.TransformationService.Models;

public sealed record LookupData
{
    public string Mappings { get; init; } = string.Empty;
    public bool IsCaseSensitive { get; init; } = true;
    public string? DefaultValue { get; init; }
}
