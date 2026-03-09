using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

namespace Transflo.Platform.Transformer.TransformationService.Services;

public class JsonParserService : IJsonParserService
{
    private readonly ILogger<JsonParserService> _logger;

    public JsonParserService(ILogger<JsonParserService> logger)
    {
        _logger = logger;
    }

    public async Task<Dictionary<string, FieldMetadata>> ExtractFieldPathsAsync(
        string jsonString,
        bool includeSampleValues = true)
    {
        var fields = new Dictionary<string, FieldMetadata>();

        try
        {
            using var document = JsonDocument.Parse(jsonString);
            ExtractFieldsRecursive(document.RootElement, string.Empty, fields, includeSampleValues);

            _logger.LogDebug($"Extracted {fields.Count} fields from JSON");
            return await Task.FromResult(fields);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse JSON");
            throw;
        }
    }

    private void ExtractFieldsRecursive(
        JsonElement element,
        string currentPath,
        Dictionary<string, FieldMetadata> fields,
        bool includeSampleValues)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var propertyPath = string.IsNullOrEmpty(currentPath)
                        ? property.Name
                        : $"{currentPath}.{property.Name}";

                    ExtractFieldsRecursive(property.Value, propertyPath, fields, includeSampleValues);
                }
                break;

            case JsonValueKind.Array:
                if (!string.IsNullOrEmpty(currentPath))
                {
                    fields[currentPath] = new FieldMetadata
                    {
                        Path = currentPath,
                        DataType = "Array",
                        IsArray = true,
                        IsNullable = false,
                        ArrayLength = element.GetArrayLength(),
                        SampleValue = includeSampleValues ? $"[{element.GetArrayLength()} items]" : null
                    };

                    if (element.GetArrayLength() > 0)
                    {
                        var arrayItemPath = $"{currentPath}[0]";
                        ExtractFieldsRecursive(element[0], arrayItemPath, fields, includeSampleValues);
                    }
                }
                break;

            case JsonValueKind.String:
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                if (!string.IsNullOrEmpty(currentPath))
                {
                    fields[currentPath] = new FieldMetadata
                    {
                        Path = currentPath,
                        DataType = GetDataType(element),
                        IsArray = false,
                        IsNullable = element.ValueKind == JsonValueKind.Null,
                        SampleValue = includeSampleValues ? GetSampleValue(element) : null
                    };
                }
                break;
        }
    }

    private string GetDataType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => "String",
            JsonValueKind.Number => element.TryGetInt64(out _) ? "Integer" : "Number",
            JsonValueKind.True or JsonValueKind.False => "Boolean",
            JsonValueKind.Null => "Null",
            JsonValueKind.Array => "Array",
            JsonValueKind.Object => "Object",
            _ => "Unknown"
        };
    }

    private object? GetSampleValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    public async Task<bool> ValidateJsonAsync(string jsonString)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonString);
            return await Task.FromResult(true);
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Invalid JSON");
            return await Task.FromResult(false);
        }
    }

    public async Task<object?> GetValueAtPathAsync(Dictionary<string, object> jsonObject, string jsonPath)
    {
        var segments = ParsePathSegments(jsonPath);
        object? current = jsonObject;

        foreach (var seg in segments)
        {
            if (current == null)
            {
                return null;
            }

            if (seg.IsArrayIndex)
            {
                if (current is IList<object> list)
                {
                    current = seg.Index >= 0 && seg.Index < list.Count ? list[seg.Index] : null;
                }
                else if (current is JsonElement arrayElement && arrayElement.ValueKind == JsonValueKind.Array)
                {
                    current = seg.Index >= 0 && seg.Index < arrayElement.GetArrayLength()
                        ? arrayElement[seg.Index]
                        : null;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (current is Dictionary<string, object> dict)
                {
                    var matchedKey = dict.Keys.FirstOrDefault(k =>
                        string.Equals(k, seg.Name, StringComparison.OrdinalIgnoreCase));
                    current = matchedKey != null ? dict[matchedKey] : null;
                }
                else if (current is JsonElement element && element.ValueKind == JsonValueKind.Object)
                {
                    JsonElement? matched = null;
                    foreach (var p in element.EnumerateObject())
                    {
                        if (string.Equals(p.Name, seg.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            matched = p.Value;
                            break;
                        }
                    }
                    current = matched.HasValue ? (object?)matched.Value : null;
                }
                else
                {
                    return null;
                }
            }
        }

        return await Task.FromResult(current);
    }

    public async Task SetValueAtPathAsync(Dictionary<string, object> jsonObject, string jsonPath, object value)
    {
        var segments = ParsePathSegments(jsonPath);
        if (segments.Count == 0)
        {
            await Task.CompletedTask;
            return;
        }

        object current = jsonObject;

        for (int i = 0; i < segments.Count - 1; i++)
        {
            var seg = segments[i];
            var next = segments[i + 1];

            if (seg.IsArrayIndex)
            {
                if (current is List<object> list)
                {
                    EnsureListCapacity(list, seg.Index + 1);
                    if (list[seg.Index] is not Dictionary<string, object>)
                    {
                        list[seg.Index] = new Dictionary<string, object>();
                    }
                    current = list[seg.Index];
                }
            }
            else
            {
                if (current is Dictionary<string, object> dict)
                {
                    if (!dict.ContainsKey(seg.Name))
                    {
                        dict[seg.Name] = next.IsArrayIndex
                            ? (object)new List<object>()
                            : new Dictionary<string, object>();
                    }
                    current = dict[seg.Name];
                }
            }
        }

        var last = segments[^1];
        if (last.IsArrayIndex)
        {
            if (current is List<object> lastList)
            {
                EnsureListCapacity(lastList, last.Index + 1);
                lastList[last.Index] = value;
            }
        }
        else
        {
            if (current is Dictionary<string, object> lastDict)
            {
                lastDict[last.Name] = value;
            }
        }

        await Task.CompletedTask;
    }

    private record PathSegment(string Name, bool IsArrayIndex, int Index);

    private static List<PathSegment> ParsePathSegments(string jsonPath)
    {
        var segments = new List<PathSegment>();

        foreach (var part in jsonPath.Split('.'))
        {
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            var bracketPos = part.IndexOf('[');
            if (bracketPos < 0)
            {
                segments.Add(new PathSegment(part, false, 0));
            }
            else
            {
                if (bracketPos > 0)
                {
                    segments.Add(new PathSegment(part[..bracketPos], false, 0));
                }

                var closeBracket = part.IndexOf(']', bracketPos);
                if (closeBracket > bracketPos)
                {
                    var indexStr = part[(bracketPos + 1)..closeBracket];
                    if (int.TryParse(indexStr, out var idx))
                    {
                        segments.Add(new PathSegment(string.Empty, true, idx));
                    }
                }
            }
        }

        return segments;
    }

    private static void EnsureListCapacity(List<object> list, int requiredCapacity)
    {
        while (list.Count < requiredCapacity)
        {
            list.Add(new Dictionary<string, object>());
        }
    }
}
