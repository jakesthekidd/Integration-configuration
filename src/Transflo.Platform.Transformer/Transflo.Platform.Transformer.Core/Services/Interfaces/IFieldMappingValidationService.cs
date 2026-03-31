using Transflo.Platform.Transformer.Core.DTOs;

namespace Transflo.Platform.Transformer.Core.Services.Interfaces;

public interface IFieldMappingValidationService
{
    Task<MappingValidationResult> ValidateAsync(Guid templateId, int version);
}
