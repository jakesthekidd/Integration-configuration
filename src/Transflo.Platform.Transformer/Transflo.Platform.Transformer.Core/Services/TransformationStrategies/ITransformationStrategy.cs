using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

public interface ITransformationStrategy
{
    TransformationType TransformationType { get; }
    Task<object?> ApplyAsync(TransformationContext context);
}
