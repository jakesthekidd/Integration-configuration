using System.Text.Json;
using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

public class ArrayMapTransformationStrategy : ITransformationStrategy
{
    private readonly IJsonParserService _jsonParser;
    private readonly ILogger<ArrayMapTransformationStrategy> _logger;

    public ArrayMapTransformationStrategy(
        IJsonParserService jsonParser,
        ILogger<ArrayMapTransformationStrategy> logger)
    {
        _jsonParser = jsonParser;
        _logger = logger;
    }

    public TransformationType TransformationType => TransformationType.ArrayMap;

    public async Task<object?> ApplyAsync(TransformationContext context)
    {
        var sourcePath = context.Mapping.SourcePath ?? string.Empty;
        var wildcardIndex = sourcePath.IndexOf("[*]", StringComparison.Ordinal);
        if (wildcardIndex < 0)
        {
            _logger.LogWarning("ArrayMap source path must contain [*]: {SourcePath}", sourcePath);
            return null;
        }

        var arrayName = sourcePath[..wildcardIndex];
        var itemField = wildcardIndex + 3 < sourcePath.Length ? sourcePath[(wildcardIndex + 4)..] : string.Empty;

        var arrayValue = await _jsonParser.GetValueAtPathAsync(context.SourceData, arrayName);

        IEnumerable<object?> items = arrayValue switch
        {
            JsonElement je when je.ValueKind == JsonValueKind.Array => je.EnumerateArray().Cast<object?>(),
            System.Collections.IList il => il.Cast<object?>(),
            _ => Enumerable.Empty<object?>()
        };

        var result = new List<object>();
        foreach (var item in items)
        {
            if (item == null)
            {
                continue;
            }
            object? extracted = null;

            if (!string.IsNullOrEmpty(itemField))
            {
                if (item is JsonElement elem && elem.ValueKind == JsonValueKind.Object)
                {
                    extracted = elem.TryGetProperty(itemField, out var p)
                        ? p.ValueKind == JsonValueKind.String ? p.GetString() : (object?)p.GetRawText()
                        : null;
                }
                else if (item is Dictionary<string, object> d)
                {
                    d.TryGetValue(itemField, out extracted);
                }
            }
            else
            {
                extracted = item;
            }

            if (extracted != null)
            {
                result.Add(extracted);
            }
        }

        return result.Count > 0 ? result : null;
    }
}
