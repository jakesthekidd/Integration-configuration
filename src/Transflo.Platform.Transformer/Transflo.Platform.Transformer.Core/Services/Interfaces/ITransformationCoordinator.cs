using System.Text.Json;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.TransformationService.DTOs;

namespace Transflo.Platform.Transformer.Core.Services.Interfaces;

public interface ITransformationCoordinator
{
    Task<TransformationResult> TransformAsync(string sourceJson, Guid templateId, int? version = null, TransformOptions? options = null);
    Task<TransformationResult> PreviewTransformationAsync(string sourceJson, Guid templateId, int? version = null);
    Task<BatchTransformResult> TransformBatchAsync(Guid templateId, List<JsonElement> records, int? version = null, TransformOptions? options = null);
}
