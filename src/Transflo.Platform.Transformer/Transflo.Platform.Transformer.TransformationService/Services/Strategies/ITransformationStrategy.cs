using Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

public interface ITransformationStrategy
{
    TransformationType TransformationType { get; }
    Task<object?> ApplyAsync(TransformationContext context);
}
