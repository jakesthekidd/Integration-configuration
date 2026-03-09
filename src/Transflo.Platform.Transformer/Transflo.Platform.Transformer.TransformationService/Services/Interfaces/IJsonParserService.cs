namespace Transflo.Platform.Transformer.TransformationService.Services.Interfaces;

public interface IJsonParserService
{
    Task<Dictionary<string, FieldMetadata>> ExtractFieldPathsAsync(string jsonString, bool includeSampleValues = true);
    Task<bool> ValidateJsonAsync(string jsonString);
    Task<object?> GetValueAtPathAsync(Dictionary<string, object> jsonObject, string jsonPath);
    Task SetValueAtPathAsync(Dictionary<string, object> jsonObject, string jsonPath, object value);
}
