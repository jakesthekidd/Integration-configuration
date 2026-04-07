namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record ApiClientListResponse
{
    public IEnumerable<ApiClientResponse> ApiClients { get; set; } = Enumerable.Empty<ApiClientResponse>();
    public int TotalCount { get; set; }
}
