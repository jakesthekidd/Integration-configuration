using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Services.TransformationStrategies;

public class TransformationContext
{
    public required Dictionary<string, object> SourceData { get; init; }
    public required FieldMapping Mapping { get; init; }
}
