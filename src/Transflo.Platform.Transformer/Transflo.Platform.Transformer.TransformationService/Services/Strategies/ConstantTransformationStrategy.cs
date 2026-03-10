using Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

public class ConstantTransformationStrategy : ITransformationStrategy
{
    public TransformationType TransformationType => TransformationType.Constant;

    public Task<object?> ApplyAsync(TransformationContext context)
        => Task.FromResult<object?>(context.Mapping.DefaultValue);
}
