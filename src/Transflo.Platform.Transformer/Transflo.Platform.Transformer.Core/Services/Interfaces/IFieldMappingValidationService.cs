using System.Text.Json;
using Transflo.Platform.Transformer.Core.DTOs;

namespace Transflo.Platform.Transformer.Core.Services.Interfaces;

public interface IFieldMappingValidationService
{
    /// <summary>
    /// Validates the structural integrity of field mappings for the given template version.
    /// </summary>
    Task<MappingValidationResult> ValidateAsync(Guid templateId, int version);

    /// <summary>
    /// Validates structural integrity of field mappings AND evaluates the field values
    /// from <paramref name="sourceDocument"/> against each mapping's ValidationRules.
    /// </summary>
    Task<MappingValidationResult> ValidateAsync(Guid templateId, int version, JsonElement sourceDocument);
}
