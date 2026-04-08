using System.Globalization;
using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

/// <summary>
/// Selects a DateTime source field based on a condition field value, converts the result
/// to UTC, and formats it with a configurable output format.
///
/// This combines three capabilities that no single existing strategy covers together:
/// <list type="number">
///   <item>Conditional source path selection — different fields are read depending on the value of a "condition field" (e.g. <c>stopType</c>).</item>
///   <item>Path coalescing — multiple source paths can be listed per branch; the first non-null, non-empty value wins.</item>
///   <item>DateTime UTC conversion + formatting — the resolved value is parsed, converted to UTC, and formatted.</item>
/// </list>
///
/// <b>TransformationConfig schema</b>
/// <code>
/// {
///   "ConditionField": "stopType",
///   "OutputFormat":   "yyyy-MM-ddTHH:mm:ss.ffffffZ",   // optional; defaults to ISO microseconds
///   "Branches": [
///     {
///       "Value":       "Origin",
///       "SourcePaths": ["actualPickup", "pickUpBy"]    // first non-null wins
///     },
///     {
///       "Value":       "Destination",
///       "SourcePaths": ["actualDelivery"]
///     }
///   ]
/// }
/// </code>
///
/// <b>Behaviour</b>
/// <list type="bullet">
///   <item>Branch matching is case-insensitive.</item>
///   <item>When no branch matches the condition field value, <c>null</c> is returned.</item>
///   <item>When a branch matches but all its source paths resolve to null / empty, <c>null</c> is returned.</item>
///   <item>When the resolved value cannot be parsed as a date, it is returned unchanged.</item>
///   <item>The default <c>OutputFormat</c> is <c>yyyy-MM-ddTHH:mm:ss.ffffffZ</c> (ISO 8601 UTC, microsecond precision).</item>
/// </list>
/// </summary>
public class ConditionalDateFormatTransformationStrategy : ITransformationStrategy
{
    private readonly IJsonParserService _jsonParser;

    public ConditionalDateFormatTransformationStrategy(IJsonParserService jsonParser)
    {
        _jsonParser = jsonParser;
    }

    public TransformationType TransformationType => TransformationType.ConditionalDateFormat;

    public async Task<object?> ApplyAsync(TransformationContext context)
    {
        var config = ParseConfig(context.Mapping.TransformationConfig);
        if (config is null)
        {
            return null;
        }

        if (!config.TryGetValue(TransformationConfigKeys.ConditionalDateFormat.ConditionField, out var condFieldRaw)
            || condFieldRaw?.ToString() is not { Length: > 0 } conditionField)
        {
            return null;
        }

        if (!config.TryGetValue(TransformationConfigKeys.ConditionalDateFormat.Branches, out var branchesRaw)
            || branchesRaw is not JsonElement branchesEl
            || branchesEl.ValueKind != JsonValueKind.Array
            || branchesEl.GetArrayLength() == 0)
        {
            return null;
        }

        var outputFormat = config.TryGetValue(TransformationConfigKeys.ConditionalDateFormat.OutputFormat, out var fmtRaw)
            ? fmtRaw?.ToString() ?? TransformationConfigKeys.ConditionalDateFormat.DefaultOutputFormat
            : TransformationConfigKeys.ConditionalDateFormat.DefaultOutputFormat;

        // Resolve the condition field value
        var condRaw = await _jsonParser.GetValueAtPathAsync(context.SourceData, conditionField);
        var condValue = condRaw is JsonElement je ? je.GetString() : condRaw?.ToString();

        if (string.IsNullOrEmpty(condValue))
        {
            return null;
        }

        // Find the matching branch
        foreach (var branch in branchesEl.EnumerateArray())
        {
            if (branch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!branch.TryGetProperty(TransformationConfigKeys.ConditionalDateFormat.Value, out var branchValueEl))
            {
                continue;
            }

            var branchValue = branchValueEl.GetString();
            if (!string.Equals(condValue, branchValue, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Branch matched — try source paths in order
            if (!branch.TryGetProperty(TransformationConfigKeys.ConditionalDateFormat.SourcePaths, out var pathsEl)
                || pathsEl.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var pathEl in pathsEl.EnumerateArray())
            {
                var path = pathEl.GetString();
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                var rawValue = await _jsonParser.GetValueAtPathAsync(context.SourceData, path);
                var strValue = rawValue is JsonElement sv ? sv.GetString() : rawValue?.ToString();

                if (string.IsNullOrWhiteSpace(strValue))
                {
                    continue;
                }

                return ConvertAndFormat(strValue, outputFormat);
            }

            return null; // Branch matched but no non-empty source value found
        }

        return null; // No branch matched
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static object? ConvertAndFormat(string dateStr, string outputFormat)
    {
        if (!DateTimeOffset.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return dateStr; // Unparseable — return as-is (consistent with DateFormat behaviour)
        }

        return parsed.UtcDateTime.ToString(outputFormat, CultureInfo.InvariantCulture);
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
