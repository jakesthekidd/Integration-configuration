using System.Globalization;
using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

/// <summary>
/// Resolves a DateTime value, converts it to UTC, and formats it with a configurable
/// output format. Supports two operating modes:
///
/// ──────────────────────────────────────────────────────────────────────────────
/// <b>Mode 1 — Coalesce (top-level SourcePaths)</b>
/// ──────────────────────────────────────────────────────────────────────────────
/// Tries each path in order; the first non-null, non-empty value wins and is
/// converted to UTC. No condition field or branches are needed.
///
/// Use this when the logic is simply:
///   "Use field A if it has a value, otherwise fall back to field B, and convert
///    whichever one wins to UTC."
///
/// <code>
/// {
///   "SourcePaths":  ["actualPickup", "pickUpBy"],
///   "OutputFormat": "yyyy-MM-ddTHH:mm:ss.ffffffZ"   // optional
/// }
/// </code>
///
/// ──────────────────────────────────────────────────────────────────────────────
/// <b>Mode 2 — Condition field + branches</b>
/// ──────────────────────────────────────────────────────────────────────────────
/// Reads a condition field (e.g. <c>stopType</c>), matches its value against the
/// <c>Branches</c> array, and within the matched branch tries <c>SourcePaths</c>
/// in order. Useful when the source field itself changes depending on a context value.
///
/// <code>
/// {
///   "ConditionField": "stopType",
///   "OutputFormat":   "yyyy-MM-ddTHH:mm:ss.ffffffZ",
///   "Branches": [
///     { "Value": "Origin",      "SourcePaths": ["actualPickup", "pickUpBy"] },
///     { "Value": "Destination", "SourcePaths": ["actualDelivery"] }
///   ]
/// }
/// </code>
///
/// ──────────────────────────────────────────────────────────────────────────────
/// <b>Shared behaviour (both modes)</b>
/// <list type="bullet">
///   <item>The first non-null, non-empty path value wins.</item>
///   <item>The resolved value is parsed and converted to UTC before formatting.</item>
///   <item>When the value cannot be parsed as a date it is returned unchanged.</item>
///   <item>Default <c>OutputFormat</c> is <c>yyyy-MM-ddTHH:mm:ss.ffffffZ</c>.</item>
///   <item>Mode 1 takes precedence when <c>SourcePaths</c> exists at the root level.</item>
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

        var outputFormat = ResolveOutputFormat(config);

        // ── Mode 1: top-level SourcePaths coalesce ────────────────────────────
        if (config.TryGetValue(TransformationConfigKeys.ConditionalDateFormat.SourcePaths, out var topPathsRaw)
            && topPathsRaw is JsonElement topPathsEl
            && topPathsEl.ValueKind == JsonValueKind.Array
            && topPathsEl.GetArrayLength() > 0)
        {
            return await CoalesceAndConvertAsync(topPathsEl, outputFormat, context.SourceData);
        }

        // ── Mode 2: condition field + branches ────────────────────────────────
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

        var condRaw = await _jsonParser.GetValueAtPathAsync(context.SourceData, conditionField);
        var condValue = condRaw is JsonElement je ? je.GetString() : condRaw?.ToString();

        if (string.IsNullOrEmpty(condValue))
        {
            return null;
        }

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

            if (!string.Equals(condValue, branchValueEl.GetString(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!branch.TryGetProperty(TransformationConfigKeys.ConditionalDateFormat.SourcePaths, out var pathsEl)
                || pathsEl.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            return await CoalesceAndConvertAsync(pathsEl, outputFormat, context.SourceData);
        }

        return null; // No branch matched
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    private async Task<object?> CoalesceAndConvertAsync(
        JsonElement pathsEl,
        string outputFormat,
        Dictionary<string, object> sourceData)
    {
        foreach (var pathEl in pathsEl.EnumerateArray())
        {
            var path = pathEl.GetString();
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            var rawValue = await _jsonParser.GetValueAtPathAsync(sourceData, path);
            var strValue = rawValue is JsonElement sv ? sv.GetString() : rawValue?.ToString();

            if (string.IsNullOrWhiteSpace(strValue))
            {
                continue;
            }

            return ConvertAndFormat(strValue, outputFormat);
        }

        return null;
    }

    private static string ResolveOutputFormat(Dictionary<string, object> config) =>
        config.TryGetValue(TransformationConfigKeys.ConditionalDateFormat.OutputFormat, out var fmtRaw)
            ? fmtRaw?.ToString() ?? TransformationConfigKeys.ConditionalDateFormat.DefaultOutputFormat
            : TransformationConfigKeys.ConditionalDateFormat.DefaultOutputFormat;

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
