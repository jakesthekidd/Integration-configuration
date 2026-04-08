using System.Text.Json;
using System.Text.RegularExpressions;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

/// <summary>
/// Builds a string from a template containing <c>{{path}}</c> placeholders that are
/// resolved from the source document at runtime.
///
/// <b>TransformationConfig schema</b>
/// <code>
/// {
///   "Template": "Order {{order.id}} for {{customer.name}} — mode: {{shipment.mode}}"
/// }
/// </code>
///
/// Each <c>{{path}}</c> placeholder is replaced with the value found at that dot-notation
/// path in the source document. Unresolved paths are replaced with an empty string.
/// </summary>
public class TemplateTransformationStrategy : ITransformationStrategy
{
    private static readonly Regex PlaceholderPattern =
        new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private readonly IJsonParserService _jsonParser;

    public TemplateTransformationStrategy(IJsonParserService jsonParser)
    {
        _jsonParser = jsonParser;
    }

    public TransformationType TransformationType => TransformationType.Template;

    public async Task<object?> ApplyAsync(TransformationContext context)
    {
        var config = ParseConfig(context.Mapping.TransformationConfig);
        if (config is null
            || !config.TryGetValue(TransformationConfigKeys.Template.TemplateKey, out var templateRaw)
            || templateRaw?.ToString() is not { Length: > 0 } template)
        {
            return null;
        }

        var result = template;

        foreach (Match match in PlaceholderPattern.Matches(template))
        {
            var path = match.Groups[1].Value.Trim();
            var raw = await _jsonParser.GetValueAtPathAsync(context.SourceData, path);
            var value = raw is JsonElement je ? je.GetString() : raw?.ToString();
            result = result.Replace(match.Value, value ?? string.Empty);
        }

        return result;
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
