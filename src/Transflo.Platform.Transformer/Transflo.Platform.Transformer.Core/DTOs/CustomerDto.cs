namespace Transflo.Platform.Transformer.Core.DTOs;

public sealed record CustomerRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string TmsName { get; set; } = string.Empty;
    public string LastSyncTime { get; set; } = string.Empty;
    public string? UpdateOrInsertStatuses { get; set; }
    public string? UpdateOnlyStatuses { get; set; }
    public Dictionary<string, string> Credentials { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string>? Settings { get; set; }
    public int? SyncFrequencyMinutes { get; set; }
    public int? OrderRetentionDays { get; set; }
    public bool Enabled { get; set; }
    public bool OutboundEnabled { get; set; }
    public string? TonuCode { get; set; }
    public string? WhiteListedOrders { get; set; }
    public int? SyncBatchSize { get; set; }
}