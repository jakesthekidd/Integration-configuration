using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

public class ConcatTransformationStrategy : ITransformationStrategy
{
    private readonly IJsonParserService _jsonParser;

    public ConcatTransformationStrategy(IJsonParserService jsonParser)
    {
        _jsonParser = jsonParser;
    }

    public TransformationType TransformationType => TransformationType.Concat;

    public async Task<object?> ApplyAsync(TransformationContext context)
    {
        var config = ParseConfig(context.Mapping.TransformationConfig);
        if (config == null)
        {
            return null;
        }

        if (!config.TryGetValue(TransformationConfigKeys.Concat.Fields, out var f) || f == null)
        {
            return null;
        }

        var fieldPaths = f is JsonElement je && je.ValueKind == JsonValueKind.Array
            ? je.EnumerateArray().Select(e => e.GetString() ?? string.Empty).ToList()
            : new List<string>();

        var separator = config.TryGetValue(TransformationConfigKeys.Concat.Separator, out var sep)
            ? sep?.ToString() ?? TransformationConfigKeys.Concat.DefaultSeparator
            : TransformationConfigKeys.Concat.DefaultSeparator;

        var skipEmpty = config.TryGetValue(TransformationConfigKeys.Concat.SkipEmpty, out var skip)
            && skip?.ToString()?.ToLowerInvariant() == TransformationConfigKeys.BoolTrue;

        var values = new List<string>();
        foreach (var path in fieldPaths)
        {
            var val = await _jsonParser.GetValueAtPathAsync(context.SourceData, path);
            var str = val is JsonElement elem ? elem.GetString() : val?.ToString();
            if (skipEmpty && string.IsNullOrWhiteSpace(str))
            {
                continue;
            }
            if (str != null)
            {
                values.Add(str);
            }
        }

        return string.Join(separator, values);
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
