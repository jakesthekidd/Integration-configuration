namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record LookupTableListResponse
{
    public List<LookupTableResponse> LookupTables { get; set; } = new();
    public int TotalCount { get; set; }
}
