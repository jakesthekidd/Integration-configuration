using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

public class DirectTransformationStrategy : ITransformationStrategy
{
    private readonly IJsonParserService _jsonParser;

    public DirectTransformationStrategy(IJsonParserService jsonParser)
    {
        _jsonParser = jsonParser;
    }

    public TransformationType TransformationType => TransformationType.Direct;

    public Task<object?> ApplyAsync(TransformationContext context)
        => _jsonParser.GetValueAtPathAsync(context.SourceData, context.Mapping.SourcePath);
}
