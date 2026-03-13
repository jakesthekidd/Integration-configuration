namespace Transflo.Platform.Transformer.Core.Configurations;

public sealed record ExternalApiOptions
{
    public string BaseUrl { get; set; }
    public string ApiKey { get; set; }
}
