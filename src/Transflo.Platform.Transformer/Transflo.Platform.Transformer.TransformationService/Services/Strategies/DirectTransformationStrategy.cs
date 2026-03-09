using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

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
