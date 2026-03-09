using System.Globalization;
using System.Text.Json;
using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

public class DateFormatTransformationStrategy : ITransformationStrategy
{
    private readonly IJsonParserService _jsonParser;
    private readonly ILogger<DateFormatTransformationStrategy> _logger;

    public DateFormatTransformationStrategy(
        IJsonParserService jsonParser,
        ILogger<DateFormatTransformationStrategy> logger)
    {
        _jsonParser = jsonParser;
        _logger = logger;
    }

    public TransformationType TransformationType => TransformationType.DateFormat;

    public async Task<object?> ApplyAsync(TransformationContext context)
    {
        var rawValue = await _jsonParser.GetValueAtPathAsync(context.SourceData, context.Mapping.SourcePath);
        var config = ParseConfig(context.Mapping.TransformationConfig);
        return ApplyDateFormat(rawValue, config);
    }

    public object? ApplyDateFormat(object? rawValue, Dictionary<string, object>? config)
    {
        if (rawValue == null)
        {
            return null;
        }

        var input = rawValue is JsonElement je ? je.GetString() : rawValue.ToString();
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var inputFormat = config?.TryGetValue("DateInputFormat", out var inf) == true ? inf?.ToString() : null;
        var outputFormat = config?.TryGetValue("DateOutputFormat", out var outf) == true
            ? outf?.ToString() ?? "o"
            : "o";

        DateTimeOffset parsed;

        if (!string.IsNullOrEmpty(inputFormat))
        {
            var formatsToTry = new[] { inputFormat, inputFormat.Replace("zzz", "zz") };
            if (!DateTimeOffset.TryParseExact(input, formatsToTry,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out parsed))
            {
                if (!DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out parsed))
                {
                    _logger.LogWarning("Could not parse date '{Input}' with format '{Format}'", input, inputFormat);
                    return input;
                }
            }
        }
        else
        {
            if (!DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out parsed))
            {
                _logger.LogWarning("Could not parse date '{Input}'", input);
                return input;
            }
        }

        return outputFormat == "o"
            ? parsed.UtcDateTime.ToString("o")
            : parsed.ToString(outputFormat, CultureInfo.InvariantCulture);
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
