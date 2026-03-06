using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

public class ConstantTransformationStrategy : ITransformationStrategy
{
    public TransformationType TransformationType => TransformationType.Constant;

    public Task<object?> ApplyAsync(TransformationContext context)
        => Task.FromResult<object?>(context.Mapping.DefaultValue);
}
