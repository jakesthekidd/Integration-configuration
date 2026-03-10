using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.Models;
using TransSvc = Transflo.Platform.Transformer.TransformationService.Services;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services;

public class TransformationServiceTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly Mock<ITransformationStrategyFactory> _strategyFactoryMock = new();
    private readonly Mock<ILogger<TransSvc.TransformationService>> _loggerMock = new();
    private readonly TransSvc.TransformationService _sut;

    private static readonly FieldMappingTemplate DefaultTemplate = new()
    {
        TemplateId = "tmpl-1",
        TmsSystemId = "sys-1",
        Name = "Test Template",
        Version = 1
    };

    public TransformationServiceTests()
    {
        _sut = new TransSvc.TransformationService(
            _jsonParserMock.Object,
            _strategyFactoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenSourceJsonIsUnparseable()
    {
        var mappings = new List<FieldMapping> { new() { SourcePath = "a", TargetPath = "b" } };

        var result = await _sut.TransformAsync("not-valid-json", DefaultTemplate, mappings);

        Assert.False(result.Success);
        Assert.Equal("TRANSFORMATION_ERROR", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenSourceJsonDeserializesToNull()
    {
        var mappings = new List<FieldMapping> { new() { SourcePath = "a", TargetPath = "b" } };

        var result = await _sut.TransformAsync("null", DefaultTemplate, mappings);

        Assert.False(result.Success);
        Assert.Equal("INVALID_JSON", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsSuccess_WithMappedFields()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "name",
            TargetPath = "fullName",
            TransformationType = TransformationType.Direct
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync("John Doe");
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);
        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "fullName", "John Doe"))
            .Returns(Task.CompletedTask);

        var result = await _sut.TransformAsync("""{"name":"John Doe"}""", DefaultTemplate, new List<FieldMapping> { mapping });

        Assert.True(result.Success);
        Assert.Equal(1, result.FieldsMapped);
        Assert.Equal(0, result.FieldsSkipped);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task TransformAsync_ReturnsFailure_WhenRequiredFieldMissing()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "requiredField",
            TargetPath = "target",
            TransformationType = TransformationType.Direct,
            IsRequired = true
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync((object?)null);
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);

        var result = await _sut.TransformAsync("""{"otherField":"value"}""", DefaultTemplate, new List<FieldMapping> { mapping });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ErrorCode == "REQUIRED_FIELD_MISSING");
    }

    [Fact]
    public async Task TransformAsync_UsesDefaultValue_WhenStrategyReturnsNull()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "optionalField",
            TargetPath = "target",
            TransformationType = TransformationType.Direct,
            DefaultValue = "fallback-value",
            IsRequired = false
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync((object?)null);
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);
        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "target", "fallback-value"))
            .Returns(Task.CompletedTask);

        var result = await _sut.TransformAsync("""{"otherField":"value"}""", DefaultTemplate, new List<FieldMapping> { mapping });

        Assert.True(result.Success);
        Assert.Equal(1, result.FieldsMapped);
        _jsonParserMock.Verify(
            p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "target", "fallback-value"),
            Times.Once);
    }

    [Fact]
    public async Task TransformAsync_FallsBackToDirect_WhenStrategyNotFound()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "someField",
            TargetPath = "target",
            TransformationType = TransformationType.Math
        };

        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Math))
            .Returns((ITransformationStrategy?)null);
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "someField"))
            .ReturnsAsync("rawValue");
        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "target", "rawValue"))
            .Returns(Task.CompletedTask);

        var result = await _sut.TransformAsync("""{"someField":"rawValue"}""", DefaultTemplate, new List<FieldMapping> { mapping });

        Assert.True(result.Success);
        Assert.Single(result.Warnings);
        Assert.Equal("UNSUPPORTED_TRANSFORMATION_TYPE", result.Warnings[0].Code);
        Assert.Equal(1, result.FieldsMapped);
    }

    [Fact]
    public async Task TransformAsync_AddsWarning_WhenOptionalFieldMissing()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "optionalField",
            TargetPath = "target",
            TransformationType = TransformationType.Direct,
            IsRequired = false
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync((object?)null);
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);

        var result = await _sut.TransformAsync("{}", DefaultTemplate, new List<FieldMapping> { mapping });

        Assert.True(result.Success);
        Assert.Equal(0, result.FieldsMapped);
        Assert.Equal(1, result.FieldsSkipped);
        Assert.Contains(result.Warnings, w => w.Code == "FIELD_VALUE_MISSING");
    }

    [Fact]
    public async Task TransformAsync_NoWarning_ForConstantType_WhenValueIsNull()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "target",
            TransformationType = TransformationType.Constant,
            DefaultValue = null
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync((object?)null);
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Constant))
            .Returns(strategyMock.Object);

        var result = await _sut.TransformAsync("{}", DefaultTemplate, new List<FieldMapping> { mapping });

        Assert.DoesNotContain(result.Warnings, w => w.Code == "FIELD_VALUE_MISSING");
    }

    [Fact]
    public async Task TransformBatchAsync_ProcessesAllRecords()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "id",
            TargetPath = "recordId",
            TransformationType = TransformationType.Direct
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync("some-id");
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);
        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "recordId", "some-id"))
            .Returns(Task.CompletedTask);

        var records = new List<JsonElement>
        {
            JsonDocument.Parse("""{"id":"r1"}""").RootElement,
            JsonDocument.Parse("""{"id":"r2"}""").RootElement,
            JsonDocument.Parse("""{"id":"r3"}""").RootElement
        };

        var batchResult = await _sut.TransformBatchAsync(DefaultTemplate, new List<FieldMapping> { mapping }, records);

        Assert.Equal(3, batchResult.TotalRecords);
        Assert.Equal(3, batchResult.Results.Count);
        Assert.Equal(3, batchResult.SuccessCount);
        Assert.Equal(0, batchResult.ErrorCount);
    }

    [Fact]
    public async Task TransformBatchAsync_DoesNotPersistLog()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "id",
            TargetPath = "recordId",
            TransformationType = TransformationType.Direct
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync("val");
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);
        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "recordId", "val"))
            .Returns(Task.CompletedTask);

        var records = new List<JsonElement>
        {
            JsonDocument.Parse("""{"id":"r1"}""").RootElement,
            JsonDocument.Parse("""{"id":"r2"}""").RootElement
        };

        await _sut.TransformBatchAsync(DefaultTemplate, new List<FieldMapping> { mapping }, records);

        _jsonParserMock.Verify(
            p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "recordId", "val"),
            Times.Exactly(2));
    }
}
