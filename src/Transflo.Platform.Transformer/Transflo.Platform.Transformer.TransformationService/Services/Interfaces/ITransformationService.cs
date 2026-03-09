using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.DTOs;
using Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

public interface ITransformationService
{
    Task<TransformationResult> TransformAsync(string sourceJson, FieldMappingTemplate template, List<FieldMapping> mappings);
    Task<BatchTransformResult> TransformBatchAsync(FieldMappingTemplate template, List<FieldMapping> mappings, List<JsonElement> records);
}
