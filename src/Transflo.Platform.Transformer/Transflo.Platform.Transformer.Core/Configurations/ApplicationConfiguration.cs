namespace Transflo.Platform.Transformer.Core.Configurations;

public sealed record ApplicationConfiguration
{
    public ConnectionStrings ConnectionStrings { get; set; } = new();
    public ExternalApis ExternalApis { get; set; } = new();
    public Cors Cors { get; set; } = new();
}

public sealed record ConnectionStrings
{
    public string DefaultConnection { get; set; } = string.Empty;
}

public sealed record ExternalApis
{
    public CustomersApi CustomersApi { get; set; } = new();
}

public sealed record CustomersApi
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public sealed record Cors
{
    public string[] AllowedOrigins { get; set; } = [];
}


