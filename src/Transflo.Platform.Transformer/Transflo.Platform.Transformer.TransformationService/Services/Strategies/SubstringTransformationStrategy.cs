using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

/// <summary>
/// Extracts a portion of a source field's string value.
///
/// <b>TransformationConfig schema</b>
/// <code>
/// {
///   "Start":  0,   // required — 0-based character index to begin extraction
///   "Length": 5    // optional — number of characters to extract; omit to take through end of string
/// }
/// </code>
///
/// Out-of-range values are clamped to the string boundaries rather than throwing.
/// Returns the original value unchanged when config is absent or <c>Start</c> is missing.
/// </summary>
public class SubstringTransformationStrategy : ITransformationStrategy
{
    private readonly IJsonParserService _jsonParser;

    public SubstringTransformationStrategy(IJsonParserService jsonParser)
    {
        _jsonParser = jsonParser;
    }

    public TransformationType TransformationType => TransformationType.Substring;

    public async Task<object?> ApplyAsync(TransformationContext context)
    {
        var raw = await _jsonParser.GetValueAtPathAsync(context.SourceData, context.Mapping.SourcePath);
        var input = raw is JsonElement je ? je.GetString() : raw?.ToString();

        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var config = ParseConfig(context.Mapping.TransformationConfig);
        if (config is null
            || !config.TryGetValue(TransformationConfigKeys.Substring.Start, out var startRaw)
            || startRaw is not JsonElement startEl
            || !startEl.TryGetInt32(out var start))
        {
            return input;
        }

        start = Math.Clamp(start, 0, input.Length);

        if (config.TryGetValue(TransformationConfigKeys.Substring.Length, out var lengthRaw)
            && lengthRaw is JsonElement lengthEl
            && lengthEl.TryGetInt32(out var length)
            && length > 0)
        {
            length = Math.Min(length, input.Length - start);
            return input.Substring(start, length);
        }

        return input[start..];
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
