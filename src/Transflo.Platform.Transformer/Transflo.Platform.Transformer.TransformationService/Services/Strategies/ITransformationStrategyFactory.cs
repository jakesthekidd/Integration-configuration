using Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

public interface ITransformationStrategyFactory
{
    ITransformationStrategy? GetStrategy(TransformationType type);
}
