namespace Transflo.Platform.Transformer.TransformationService.Services;

public class FieldMetadata
{
    public string Path { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsArray { get; set; }
    public bool IsNullable { get; set; }
    public object? SampleValue { get; set; }
    public int? ArrayLength { get; set; }
}
