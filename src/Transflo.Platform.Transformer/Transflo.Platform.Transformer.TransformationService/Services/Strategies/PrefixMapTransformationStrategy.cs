using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

/// <summary>
/// Collects all source properties whose names share a common prefix and builds an
/// array of structured objects by splitting each value with a configurable separator.
///
/// <b>Use case</b>: A flat source document contains multiple numbered fields such as
/// <c>deliveryDriver1 = "John Doe"</c>, <c>deliveryDriver2 = "Jane Smith"</c>.
/// <c>PrefixMap</c> gathers all of them, splits each value, and emits a typed array:
/// <code>
/// [
///   { "firstName": "John",  "lastName": "Doe"   },
///   { "firstName": "Jane",  "lastName": "Smith"  }
/// ]
/// </code>
///
/// <b>SourcePath</b> — the common prefix to match (e.g. <c>deliveryDriver</c>).
/// Only top-level source keys that <i>start with</i> this prefix and have at least one
/// additional character are included. Matched keys are sorted lexicographically so that
/// <c>deliveryDriver1</c> always precedes <c>deliveryDriver2</c>.
///
/// <b>TransformationConfig schema</b>
/// <code>
/// {
///   "Fields":    ["firstName", "lastName"],  // required — names for each split part
///   "Separator": " ",                        // optional — defaults to a single space
///   "SkipEmpty": "true"                      // optional — omit entries with null/whitespace values
/// }
/// </code>
///
/// If a value produces fewer parts than <c>Fields</c>, the remaining field names are
/// mapped to <c>null</c>. Extra parts beyond <c>Fields.Length</c> are ignored.
/// </summary>
public class PrefixMapTransformationStrategy : ITransformationStrategy
{
    public TransformationType TransformationType => TransformationType.PrefixMap;

    public Task<object?> ApplyAsync(TransformationContext context)
    {
        var prefix = context.Mapping.SourcePath;
        if (string.IsNullOrEmpty(prefix))
        {
            return Task.FromResult<object?>(null);
        }

        var config = ParseConfig(context.Mapping.TransformationConfig);
        if (config is null)
        {
            return Task.FromResult<object?>(null);
        }

        if (!config.TryGetValue(TransformationConfigKeys.PrefixMap.Fields, out var fieldsRaw)
            || fieldsRaw is not JsonElement fieldsEl
            || fieldsEl.ValueKind != JsonValueKind.Array
            || fieldsEl.GetArrayLength() == 0)
        {
            return Task.FromResult<object?>(null);
        }

        var fieldNames = fieldsEl.EnumerateArray()
            .Select(e => e.GetString() ?? string.Empty)
            .Where(s => s.Length > 0)
            .ToList();

        if (fieldNames.Count == 0)
        {
            return Task.FromResult<object?>(null);
        }

        var separator = config.TryGetValue(TransformationConfigKeys.PrefixMap.Separator, out var sepRaw)
            ? sepRaw?.ToString() ?? TransformationConfigKeys.PrefixMap.DefaultSeparator
            : TransformationConfigKeys.PrefixMap.DefaultSeparator;

        var skipEmpty = config.TryGetValue(TransformationConfigKeys.PrefixMap.SkipEmpty, out var skipRaw)
            && skipRaw?.ToString()?.ToLowerInvariant() == TransformationConfigKeys.BoolTrue;

        var matchingKeys = context.SourceData.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && k.Length > prefix.Length)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new List<Dictionary<string, object?>>();
        foreach (var key in matchingKeys)
        {
            var raw = context.SourceData[key];
            var strValue = raw is JsonElement je ? je.GetString() : raw?.ToString();

            if (skipEmpty && string.IsNullOrWhiteSpace(strValue))
            {
                continue;
            }

            var parts = strValue?.Split(separator, StringSplitOptions.None) ?? [];
            var obj = new Dictionary<string, object?>();
            for (int i = 0; i < fieldNames.Count; i++)
            {
                obj[fieldNames[i]] = i < parts.Length ? parts[i] : null;
            }
            result.Add(obj);
        }

        return Task.FromResult<object?>(result.Count > 0 ? (object)result : null);
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
