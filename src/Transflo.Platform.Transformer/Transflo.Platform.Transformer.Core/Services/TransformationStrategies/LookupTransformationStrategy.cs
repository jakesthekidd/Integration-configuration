using System.Text.Json;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;

namespace Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

public class LookupTransformationStrategy : ITransformationStrategy
{
    private readonly IJsonParserService _jsonParser;
    private readonly ILookupTableRepository _lookupRepository;
    private readonly ILogger<LookupTransformationStrategy> _logger;

    public LookupTransformationStrategy(
        IJsonParserService jsonParser,
        ILookupTableRepository lookupRepository,
        ILogger<LookupTransformationStrategy> logger)
    {
        _jsonParser = jsonParser;
        _lookupRepository = lookupRepository;
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
        if (config == null || !config.ContainsKey("LookupTableId"))
        {
            return sourceValue;
        }

        var lookupTableId = config["LookupTableId"]?.ToString();
        if (string.IsNullOrEmpty(lookupTableId))
        {
            return sourceValue;
        }

        var lookupTable = await _lookupRepository.GetByIdAsync(lookupTableId);
        if (lookupTable?.Mappings == null)
        {
            _logger.LogWarning("Lookup table not found: {LookupTableId}", lookupTableId);
            return sourceValue;
        }

        var rawMappings = JsonSerializer.Deserialize<Dictionary<string, string>>(lookupTable.Mappings);
        if (rawMappings == null)
        {
            return sourceValue;
        }

        var comparer = lookupTable.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var mappings = new Dictionary<string, string>(rawMappings, comparer);
        var key = sourceValue.ToString() ?? string.Empty;

        return mappings.TryGetValue(key, out var value)
            ? value
            : lookupTable.DefaultValue ?? sourceValue;
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
