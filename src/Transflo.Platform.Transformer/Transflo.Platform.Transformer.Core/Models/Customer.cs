using Amazon.DynamoDBv2.DataModel;
using System.Text.Json.Serialization;
using Transflo.Platform.Transformer.Core.Models;

[DynamoDBTable("Customers")]
public class Customer
{
    [DynamoDBHashKey]
    public string CustomerId { get; set; }

    [DynamoDBProperty]
    public string TmsName { get; set; }

    [DynamoDBProperty]
    public string LastSyncTime { get; set; }

    [DynamoDBIgnore]
    [JsonIgnore]
    public string[] UpdateOrInsertStatusesList
    {
        get
        {
            return string.IsNullOrWhiteSpace(UpdateOrInsertStatuses) ? []
            : UpdateOrInsertStatuses.Split(",");
        }
    }

    [DynamoDBProperty]
    public string? UpdateOrInsertStatuses { get; set; }

    [DynamoDBIgnore]
    [JsonIgnore]
    public string[] UpdateOnlyStatusesList
    {
        get
        {
            return string.IsNullOrWhiteSpace(UpdateOnlyStatuses) ? []
            : UpdateOnlyStatuses.Split(",");
        }
    }

    [DynamoDBProperty]
    public string? UpdateOnlyStatuses { get; set; }

    [DynamoDBIgnore]
    [JsonIgnore]
    public SecretData SecretData { get; set; }

    [DynamoDBProperty]
    public string CustomerName { get; set; }

    [DynamoDBIgnore]
    public Dictionary<string, string> Credentials { get; set; }

    [DynamoDBProperty]
    public Dictionary<string, string>? Settings { get; set; }

    [DynamoDBProperty]
    public int? SyncFrequencyMinutes { get; set; }

    [DynamoDBProperty]
    public int? OrderRetentionDays { get; set; }

    [DynamoDBProperty]
    public bool Enabled { get; set; }

    [DynamoDBProperty]
    public bool IsDeleted { get; set; }

    [DynamoDBProperty]
    public string? TonuCode { get; set; }

    [DynamoDBProperty]
    public bool OutboundEnabled { get; set; }

    [DynamoDBProperty]
    public string? WhiteListedOrders { get; set; }

    [DynamoDBProperty]
    public int? SyncBatchSize { get; set; }
}
