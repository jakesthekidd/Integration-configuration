using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

public class LookupTransformationStrategy : ITransformationStrategy
{
    private readonly IJsonParserService _jsonParser;
    private readonly ILookupDataProvider _lookupProvider;
    private readonly ILogger<LookupTransformationStrategy> _logger;

    public LookupTransformationStrategy(
        IJsonParserService jsonParser,
        ILookupDataProvider lookupProvider,
        ILogger<LookupTransformationStrategy> logger)
    {
        _jsonParser = jsonParser;
        _lookupProvider = lookupProvider;
        _logger = logger;
    }

    public TransformationType TransformationType => TransformationType.Lookup;

    public async Task<object?> ApplyAsync(TransformationContext context)
    {
        var sourceValue = await _jsonParser.GetValueAtPathAsync(context.SourceData, context.Mapping.SourcePath);
        if (sourceValue == null)
        {
            return null;
        }

        var config = ParseConfig(context.Mapping.TransformationConfig);
        if (config == null || !config.ContainsKey(TransformationConfigKeys.Lookup.LookupTableId))
        {
            return sourceValue;
        }

        if (!config.TryGetValue(TransformationConfigKeys.Lookup.LookupTableId, out var lookupTableIdRaw) ||
            lookupTableIdRaw == null ||
            !Guid.TryParse(lookupTableIdRaw.ToString(), out var lookupTableId))
        {
            return sourceValue;
        }

        var lookupData = await _lookupProvider.GetAsync(lookupTableId);
        if (lookupData == null || string.IsNullOrEmpty(lookupData.Mappings))
        {
            _logger.LogWarning("Lookup table not found or has no mappings: {LookupTableId}", lookupTableId);
            return sourceValue;
        }

        var rawMappings = JsonSerializer.Deserialize<Dictionary<string, string>>(lookupData.Mappings);
        if (rawMappings == null)
        {
            return sourceValue;
        }

        var comparer = lookupData.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var mappings = new Dictionary<string, string>(rawMappings, comparer);
        var key = sourceValue.ToString() ?? string.Empty;

        return mappings.TryGetValue(key, out var value)
            ? value
            : lookupData.DefaultValue ?? sourceValue;
    }

    private static Dictionary<string, object>? ParseConfig(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
        {
            return null;
        }
        try { return JsonSerializer.Deserialize<Dictionary<string, object>>(configJson); }
        catch { return null; }
    }
}
