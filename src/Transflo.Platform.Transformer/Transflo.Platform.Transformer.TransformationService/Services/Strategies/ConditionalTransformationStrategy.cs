using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

/// <summary>
/// Evaluates conditions and returns an output value based on the combined result.
///
/// Supports two config shapes:
///
/// <b>1. Flat — all conditions share one logic operator</b>
/// <code>
/// {
///   "Conditions":     [ { "Field": "status", "Operator": "equals", "Value": "ACTIVE" } ],
///   "ConditionLogic": "AND",         // "AND" (default) or "OR"
///   "TrueValue":  "Active",          // static literal  — OR —
///   "TruePath":   "source.field",    // path resolved from source data (takes precedence)
///   "FalseValue": "Inactive",
///   "FalsePath":  "other.field"
/// }
/// </code>
///
/// <b>2. Grouped — mix AND and OR across groups</b>
/// <code>
/// {
///   "ConditionGroups": [
///     {
///       "Logic": "AND",
///       "Conditions": [
///         { "Field": "status", "Operator": "equals", "Value": "ACTIVE" },
///         { "Field": "mode",   "Operator": "equals", "Value": "TL"     }
///       ]
///     },
///     {
///       "Logic": "AND",
///       "Conditions": [
///         { "Field": "priority", "Operator": "equals", "Value": "HIGH" }
///       ]
///     }
///   ],
///   "GroupLogic": "OR",
///   "TruePath":  "approved.label",
///   "FalsePath": "fallback.label"
/// }
/// </code>
/// The example above evaluates as: (status==ACTIVE AND mode==TL) OR (priority==HIGH).
///
/// <b>Output resolution</b> — same rules for both shapes:
///
/// When the condition <b>passes</b>, evaluated in order:
/// <list type="bullet">
///   <item><c>MapSourceOnTrue: true</c> — maps the <c>SourcePath</c> field value directly to the target</item>
///   <item><c>TruePath</c> — path resolved from source data at runtime</item>
///   <item><c>TrueValue</c> — static literal string</item>
/// </list>
/// When the condition <b>fails</b>:
/// <list type="bullet">
///   <item><c>FalsePath</c> — path resolved from source data at runtime</item>
///   <item><c>FalseValue</c> — static literal string</item>
/// </list>
///
/// <b>Supported operators</b> (case-insensitive):
/// equals | notequals | contains | startswith | endswith |
/// greaterthan | lessthan | greaterthanorequals | lessthanorequals |
/// isempty | isnotempty | in | notin
///
/// For <c>in</c>/<c>notin</c>, <c>Value</c> is a comma-separated list of values.
/// </summary>
public class ConditionalTransformationStrategy : ITransformationStrategy
{
    private readonly IJsonParserService _jsonParser;

    public ConditionalTransformationStrategy(IJsonParserService jsonParser)
    {
        _jsonParser = jsonParser;
    }

    public TransformationType TransformationType => TransformationType.Conditional;

    public async Task<object?> ApplyAsync(TransformationContext context)
    {
        var config = ParseConfig(context.Mapping.TransformationConfig);
        if (config is null) return null;

        bool passed;

        if (config.TryGetValue("ConditionGroups", out var groupsRaw)
            && groupsRaw is JsonElement groupsEl
            && groupsEl.ValueKind == JsonValueKind.Array
            && groupsEl.GetArrayLength() > 0)
        {
            passed = await EvaluateGroupsAsync(groupsEl, config, context.SourceData);
        }
        else if (config.TryGetValue("Conditions", out var conditionsRaw)
            && conditionsRaw is JsonElement conditionsEl
            && conditionsEl.ValueKind == JsonValueKind.Array
            && conditionsEl.GetArrayLength() > 0)
        {
            var logic = ResolveLogic(config, "ConditionLogic");
            passed = await EvaluateConditionsAsync(conditionsEl, logic, context.SourceData);
        }
        else
        {
            return null;
        }

        if (passed && IsMapSourceOnTrue(config))
            return await _jsonParser.GetValueAtPathAsync(context.SourceData, context.Mapping.SourcePath);

        return passed
            ? await ResolveOutputAsync(config, "TruePath", "TrueValue", context.SourceData)
            : await ResolveOutputAsync(config, "FalsePath", "FalseValue", context.SourceData);
    }

    // ── Group evaluation ──────────────────────────────────────────────────────

    private async Task<bool> EvaluateGroupsAsync(
        JsonElement groupsEl,
        Dictionary<string, object> config,
        Dictionary<string, object> sourceData)
    {
        var groupLogic = ResolveLogic(config, "GroupLogic");
        var groupResults = new List<bool>();

        foreach (var group in groupsEl.EnumerateArray())
        {
            if (group.ValueKind != JsonValueKind.Object) continue;

            if (!group.TryGetProperty("Conditions", out var condEl)
                || condEl.ValueKind != JsonValueKind.Array
                || condEl.GetArrayLength() == 0)
            {
                continue;
            }

            var innerLogic = group.TryGetProperty("Logic", out var logicEl)
                ? logicEl.GetString()?.ToUpperInvariant() ?? "AND"
                : "AND";

            groupResults.Add(await EvaluateConditionsAsync(condEl, innerLogic, sourceData));
        }

        if (groupResults.Count == 0) return false;

        return groupLogic == "OR"
            ? groupResults.Any(r => r)
            : groupResults.All(r => r);
    }

    // ── Flat condition list evaluation ────────────────────────────────────────

    private async Task<bool> EvaluateConditionsAsync(
        JsonElement conditionsEl,
        string logic,
        Dictionary<string, object> sourceData)
    {
        var results = new List<bool>();
        foreach (var condition in conditionsEl.EnumerateArray())
        {
            results.Add(await EvaluateConditionAsync(condition, sourceData));
        }

        return logic == "OR"
            ? results.Any(r => r)
            : results.All(r => r);
    }

    // ── Single condition ──────────────────────────────────────────────────────

    private async Task<bool> EvaluateConditionAsync(JsonElement condition, Dictionary<string, object> sourceData)
    {
        if (!condition.TryGetProperty("Field", out var fieldEl)
            || !condition.TryGetProperty("Operator", out var operatorEl))
        {
            return false;
        }

        var field = fieldEl.GetString() ?? string.Empty;
        var op = operatorEl.GetString()?.ToLowerInvariant() ?? string.Empty;

        var raw = await _jsonParser.GetValueAtPathAsync(sourceData, field);
        var fieldValue = raw is JsonElement je ? je.GetString() : raw?.ToString();

        var conditionValue = condition.TryGetProperty("Value", out var valueEl)
            ? valueEl.GetString()
            : null;

        return op switch
        {
            "equals" or "eq"
                => string.Equals(fieldValue, conditionValue, StringComparison.OrdinalIgnoreCase),

            "notequals" or "ne"
                => !string.Equals(fieldValue, conditionValue, StringComparison.OrdinalIgnoreCase),

            "contains"
                => fieldValue?.Contains(conditionValue ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true,

            "startswith"
                => fieldValue?.StartsWith(conditionValue ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true,

            "endswith"
                => fieldValue?.EndsWith(conditionValue ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true,

            "greaterthan" or "gt"
                => CompareNumeric(fieldValue, conditionValue) > 0,

            "lessthan" or "lt"
                => CompareNumeric(fieldValue, conditionValue) < 0,

            "greaterthanorequals" or "gte"
                => CompareNumeric(fieldValue, conditionValue) >= 0,

            "lessthanorequals" or "lte"
                => CompareNumeric(fieldValue, conditionValue) <= 0,

            "isempty"
                => string.IsNullOrEmpty(fieldValue),

            "isnotempty"
                => !string.IsNullOrEmpty(fieldValue),

            "in"
                => conditionValue?
                       .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                       .Any(v => string.Equals(v, fieldValue, StringComparison.OrdinalIgnoreCase)) == true,

            "notin"
                => conditionValue?
                       .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                       .All(v => !string.Equals(v, fieldValue, StringComparison.OrdinalIgnoreCase)) != false,

            _ => false
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolves the output for a branch. <paramref name="pathKey"/> (e.g. "TruePath") takes
    /// precedence over <paramref name="valueKey"/> (e.g. "TrueValue"). Returns <c>null</c>
    /// when neither is configured.
    /// </summary>
    private async Task<object?> ResolveOutputAsync(
        Dictionary<string, object> config,
        string pathKey,
        string valueKey,
        Dictionary<string, object> sourceData)
    {
        if (config.TryGetValue(pathKey, out var pathRaw) && pathRaw?.ToString() is { Length: > 0 } path)
            return await _jsonParser.GetValueAtPathAsync(sourceData, path);

        return config.TryGetValue(valueKey, out var literal) ? literal?.ToString() : null;
    }

    private static bool IsMapSourceOnTrue(Dictionary<string, object> config) =>
        config.TryGetValue("MapSourceOnTrue", out var raw)
        && raw is JsonElement el
        && el.ValueKind == JsonValueKind.True;

    private static string ResolveLogic(Dictionary<string, object> config, string key) =>
        config.TryGetValue(key, out var raw)
            ? raw?.ToString()?.ToUpperInvariant() ?? "AND"
            : "AND";

    private static int CompareNumeric(string? a, string? b)
    {
        if (double.TryParse(a, out var da) && double.TryParse(b, out var db))
            return da.CompareTo(db);

        return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, object>? ParseConfig(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson)) return null;
        try { return JsonSerializer.Deserialize<Dictionary<string, object>>(configJson); }
        catch { return null; }
    }
}
