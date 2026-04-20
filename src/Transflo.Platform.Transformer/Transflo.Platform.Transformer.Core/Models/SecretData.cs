using System.Text.Json.Serialization;

namespace Transflo.Platform.Transformer.Core.Models
{
    public class SecretData
    {
        [JsonPropertyName("mcleod-url")]
        public string McleodUrl { get; set; } = string.Empty;

        [JsonPropertyName("mcleod-auth-header")]
        public string McleodAuthHeader { get; set; } = string.Empty;

        [JsonPropertyName("company-id-header")]
        public string CompanyIdHeader { get; set; } = string.Empty;

        [JsonPropertyName("x1-url")]
        public string X1Url { get; set; } = string.Empty;

        [JsonPropertyName("x1-auth-header")]
        public string X1AuthorizationHeader { get; set; } = string.Empty;

        [JsonPropertyName("wfai-url")]
        public string WfaiUrl { get; set; } = string.Empty;

        [JsonPropertyName("wfai-integration-base-url")]
        public string WfaiIntegrationBaseUrl { get; set; } = string.Empty;

        [JsonPropertyName("wfai-portal-customer-id")]
        public string WfaiPortalCustomerId { get; set; } = string.Empty;
        public string? TonuCode { get; internal set; }

        // DMS integration fields
        [JsonPropertyName("freight-api-base-url")]
        public string? FreightApiBaseUrl { get; set; }

        [JsonPropertyName("wfai-tenant-id")]
        public string? WfaiTenantId { get; set; }

        [JsonPropertyName("dms-auth-url")]
        public string? DmsAuthUrl { get; set; }

        [JsonPropertyName("dms-add-doc-url")]
        public string? DmsAddDocUrl { get; set; }

        [JsonPropertyName("dms-username")]
        public string? DmsUsername { get; set; }

        [JsonPropertyName("dms-password")]
        public string? DmsPassword { get; set; }
    }
}
