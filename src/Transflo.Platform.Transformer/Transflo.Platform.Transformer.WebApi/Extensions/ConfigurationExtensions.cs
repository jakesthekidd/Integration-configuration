using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;
using Transflo.Platform.Transformer.Core.Configurations;

namespace Transflo.Platform.Transformer.WebApi.Extensions;

public static class ConfigurationExtensions
{
    public static async Task<WebApplicationBuilder> AddApplicationConfigurationAsync(
        this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
        {
            await LoadAwsSecretsAsync(builder);
        }

        builder.Services.Configure<ApplicationConfiguration>(builder.Configuration);

        return builder;
    }

    private static async Task LoadAwsSecretsAsync(WebApplicationBuilder builder)
    {
        // Region resolved from config/environment, not hardcoded
        var regionName = builder.Configuration["AWS:Region"] ?? RegionEndpoint.USEast1.SystemName;

        var secretId = builder.Configuration["AWS:SecretId"] ?? "platform/transformer/secrets";

        var region = RegionEndpoint.GetBySystemName(regionName);

        using var secretsClient = new AmazonSecretsManagerClient(region);

        GetSecretValueResponse secretResponse;
        try
        {
            secretResponse = await secretsClient.GetSecretValueAsync(
                new GetSecretValueRequest { SecretId = secretId });
        }
        catch (Exception ex)
        {
            // Fail fast — a missing secret at startup is not recoverable
            throw new InvalidOperationException(
                $"Failed to load secrets from AWS Secrets Manager (SecretId: '{secretId}'). " +
                $"Application cannot start.", ex);
        }

        if (secretResponse.SecretString is null)
        {
            throw new InvalidOperationException(
                $"AWS secret '{secretId}' returned an empty SecretString.");
        }

        var secretData = JsonSerializer.Deserialize<Dictionary<string, string>>(
            secretResponse.SecretString);

        if (secretData is null || secretData.Count == 0)
        {
            throw new InvalidOperationException(
                $"AWS secret '{secretId}' deserialized to an empty dictionary.");
        }

        // Use a MemoryConfigurationSource so nested keys (e.g. "Database:ConnectionString")
        // are correctly handled by the config system rather than set as flat string keys
        builder.Configuration.AddInMemoryCollection(secretData!);
    }
}