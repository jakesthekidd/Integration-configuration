using System.Text.Json;
using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

public class ArrayFlattenTransformationStrategy : ITransformationStrategy
{
    private readonly IJsonParserService _jsonParser;

    public ArrayFlattenTransformationStrategy(IJsonParserService jsonParser)
    {
        _jsonParser = jsonParser;
    }

    public TransformationType TransformationType => TransformationType.ArrayFlatten;

    public async Task<object?> ApplyAsync(TransformationContext context)
    {
        var config = ParseConfig(context.Mapping.TransformationConfig);
        var arrayName = config?.TryGetValue("SourceArrayPath", out var ap) == true ? ap?.ToString() : null;
        var itemField = config?.TryGetValue("ItemField", out var fi) == true ? fi?.ToString() : null;

        if (string.IsNullOrEmpty(arrayName))
        {
            var sourcePath = context.Mapping.SourcePath ?? string.Empty;
            var wildcardIndex = sourcePath.IndexOf("[*]", StringComparison.Ordinal);
            if (wildcardIndex >= 0)
            {
                arrayName = sourcePath[..wildcardIndex];
                itemField ??= wildcardIndex + 3 < sourcePath.Length ? sourcePath[(wildcardIndex + 4)..] : null;
            }
        }

        if (string.IsNullOrEmpty(arrayName)) return null;

        var arrayValue = await _jsonParser.GetValueAtPathAsync(context.SourceData, arrayName);

        IEnumerable<object?> items = arrayValue switch
        {
            JsonElement je when je.ValueKind == JsonValueKind.Array => je.EnumerateArray().Cast<object?>(),
            System.Collections.IList il => il.Cast<object?>(),
            _ => Enumerable.Empty<object?>()
        };

        bool filterEmpty = config?.TryGetValue("FilterEmpty", out var fe) == true
            && fe?.ToString()?.ToLowerInvariant() == "true";

        var result = new List<object>();
        foreach (var item in items)
        {
            if (item == null) continue;
            object? val = null;

            if (!string.IsNullOrEmpty(itemField))
            {
                if (item is JsonElement elem && elem.ValueKind == JsonValueKind.Object)
                    val = elem.TryGetProperty(itemField, out var p)
                        ? p.ValueKind == JsonValueKind.String ? (object?)p.GetString() : p.GetRawText()
                        : null;
                else if (item is Dictionary<string, object> d)
                    d.TryGetValue(itemField, out val);
            }
            else
            {
                val = item;
            }

            if (val == null || filterEmpty && string.IsNullOrWhiteSpace(val.ToString())) continue;
            result.Add(val);
        }

        return result.Count > 0 ? result : null;
    }

    private static Dictionary<string, object>? ParseConfig(string? configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson)) return null;
        try { return JsonSerializer.Deserialize<Dictionary<string, object>>(configJson); }
        catch { return null; }
    }
}
