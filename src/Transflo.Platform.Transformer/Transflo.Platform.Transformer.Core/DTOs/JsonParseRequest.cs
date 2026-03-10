namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record JsonParseRequest
{
    public string JsonString { get; set; } = string.Empty;
    public bool IncludeSampleValues { get; set; } = true;
}
