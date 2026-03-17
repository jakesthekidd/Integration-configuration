namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record PublishVersionRequest
{
    /// <summary>Optional identifier (e.g. user email or sub claim) of who is publishing.</summary>
    public string? PublishedBy { get; set; }
}
