using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

public interface ITransformationStrategyFactory
{
    ITransformationStrategy? GetStrategy(TransformationType type);
}

public class TransformationStrategyFactory : ITransformationStrategyFactory
{
    private readonly Dictionary<TransformationType, ITransformationStrategy> _strategies;

    public TransformationStrategyFactory(IEnumerable<ITransformationStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.TransformationType);
    }

    public ITransformationStrategy? GetStrategy(TransformationType type)
        => _strategies.TryGetValue(type, out var strategy) ? strategy : null;
}
