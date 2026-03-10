namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record TmsSystemListResponse
{
    public List<TmsSystemResponse> Systems { get; set; } = new();
    public int TotalCount { get; set; }
}
