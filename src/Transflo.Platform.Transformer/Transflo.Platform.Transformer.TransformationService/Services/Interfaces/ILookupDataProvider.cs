using Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

public interface ILookupDataProvider
{
    Task<LookupData?> GetAsync(Guid lookupTableId);
}
