using Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.TransformationService.Services.Strategies;

public class TransformationContext
{
    public required Dictionary<string, object> SourceData { get; init; }
    public required FieldMapping Mapping { get; init; }
}
