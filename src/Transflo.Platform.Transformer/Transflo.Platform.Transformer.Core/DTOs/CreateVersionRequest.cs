namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record CreateVersionRequest
{
    /// <summary>
    /// If provided, the new Draft is forked from this specific version number.
    /// If omitted, the new Draft is forked from the currently Published version.
    /// </summary>
    public int? BaseVersion { get; init; }
}
