using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories;
using Transflo.Platform.Transformer.Core.Services;
using Transflo.Platform.Transformer.Core.Services.TransformationStrategies;
using Xunit;

namespace Transflo.Platform.Transformer.Core.Tests.Services;

public class TransformationServiceTests
{
    private readonly Mock<ITemplateRepository> _templateRepoMock = new();
    private readonly Mock<IFieldMappingRepository> _mappingRepoMock = new();
    private readonly Mock<ITransformationLogRepository> _logRepoMock = new();
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly Mock<ITransformationStrategyFactory> _strategyFactoryMock = new();
    private readonly Mock<ILogger<TransformationService>> _loggerMock = new();
    private readonly TransformationService _sut;

    private static readonly FieldMappingTemplate DefaultTemplate = new()
    {
        TemplateId = "tmpl-1",
        TmsSystemId = "sys-1",
        Name = "Test Template",
        Version = 1
    };

    public TransformationServiceTests()
    {
        _sut = new TransformationService(
            _templateRepoMock.Object,
            _mappingRepoMock.Object,
            _logRepoMock.Object,
            _jsonParserMock.Object,
            _strategyFactoryMock.Object,
            _loggerMock.Object);

        _logRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<TransformationLog>()))
            .ReturnsAsync((TransformationLog log) => log);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenTemplateNotFound()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync("tmpl-missing"))
            .ReturnsAsync((FieldMappingTemplate?)null);

        var result = await _sut.TransformAsync("{}", "tmpl-missing");

        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Equal("TEMPLATE_NOT_FOUND", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenSpecificVersionNotFound()
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync("tmpl-1", 99))
            .ReturnsAsync((FieldMappingTemplate?)null);

        var result = await _sut.TransformAsync("{}", "tmpl-1", version: 99);

        Assert.False(result.Success);
        Assert.Equal("TEMPLATE_NOT_FOUND", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenNoMappingsFound()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync("tmpl-1"))
            .ReturnsAsync(DefaultTemplate);
        _mappingRepoMock
            .Setup(r => r.GetByTemplateIdOrderedAsync("tmpl-1"))
            .ReturnsAsync(new List<FieldMapping>());

        var result = await _sut.TransformAsync("{}", "tmpl-1");

        Assert.False(result.Success);
        Assert.Equal("NO_MAPPINGS", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenSourceJsonIsUnparseable()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync("tmpl-1"))
            .ReturnsAsync(DefaultTemplate);
        _mappingRepoMock
            .Setup(r => r.GetByTemplateIdOrderedAsync("tmpl-1"))
            .ReturnsAsync(new List<FieldMapping> { new() { SourcePath = "a", TargetPath = "b" } });

        var result = await _sut.TransformAsync("not-valid-json", "tmpl-1");

        Assert.False(result.Success);
        Assert.Equal("TRANSFORMATION_ERROR", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenSourceJsonDeserializesToNull()
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync("tmpl-1"))
            .ReturnsAsync(DefaultTemplate);
        _mappingRepoMock
            .Setup(r => r.GetByTemplateIdOrderedAsync("tmpl-1"))
            .ReturnsAsync(new List<FieldMapping> { new() { SourcePath = "a", TargetPath = "b" } });

        var result = await _sut.TransformAsync("null", "tmpl-1");

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
        SetupSuccessfulTransformation("tmpl-1", new List<FieldMapping> { mapping });

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync("John Doe");
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);

        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "fullName", "John Doe"))
            .Returns(Task.CompletedTask);

        var result = await _sut.TransformAsync("""{"name":"John Doe"}""", "tmpl-1");

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
        SetupSuccessfulTransformation("tmpl-1", new List<FieldMapping> { mapping });

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync((object?)null);
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);

        var result = await _sut.TransformAsync("""{"otherField":"value"}""", "tmpl-1");

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
        SetupSuccessfulTransformation("tmpl-1", new List<FieldMapping> { mapping });

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync((object?)null);
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);

        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "target", "fallback-value"))
            .Returns(Task.CompletedTask);

        var result = await _sut.TransformAsync("""{"otherField":"value"}""", "tmpl-1");

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
            TransformationType = TransformationType.Math  // not registered
        };
        SetupSuccessfulTransformation("tmpl-1", new List<FieldMapping> { mapping });

        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Math))
            .Returns((ITransformationStrategy?)null);

        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "someField"))
            .ReturnsAsync("rawValue");
        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "target", "rawValue"))
            .Returns(Task.CompletedTask);

        var result = await _sut.TransformAsync("""{"someField":"rawValue"}""", "tmpl-1");

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
        SetupSuccessfulTransformation("tmpl-1", new List<FieldMapping> { mapping });

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync((object?)null);
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);

        var result = await _sut.TransformAsync("""{}""", "tmpl-1");

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
        SetupSuccessfulTransformation("tmpl-1", new List<FieldMapping> { mapping });

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync((object?)null);
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Constant))
            .Returns(strategyMock.Object);

        var result = await _sut.TransformAsync("""{}""", "tmpl-1");

        Assert.DoesNotContain(result.Warnings, w => w.Code == "FIELD_VALUE_MISSING");
    }

    [Fact]
    public async Task PreviewTransformationAsync_DoesNotPersistLog()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "name",
            TargetPath = "fullName",
            TransformationType = TransformationType.Direct
        };
        SetupSuccessfulTransformation("tmpl-1", new List<FieldMapping> { mapping });

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync("Jane");
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);
        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "fullName", "Jane"))
            .Returns(Task.CompletedTask);

        await _sut.PreviewTransformationAsync("""{"name":"Jane"}""", "tmpl-1");

        _logRepoMock.Verify(r => r.CreateAsync(It.IsAny<TransformationLog>()), Times.Never);
    }

    [Fact]
    public async Task TransformAsync_PersistsLog_AfterTransformation()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "name",
            TargetPath = "fullName",
            TransformationType = TransformationType.Direct
        };
        SetupSuccessfulTransformation("tmpl-1", new List<FieldMapping> { mapping });

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync("Jane");
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);
        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "fullName", "Jane"))
            .Returns(Task.CompletedTask);

        await _sut.TransformAsync("""{"name":"Jane"}""", "tmpl-1");

        _logRepoMock.Verify(r => r.CreateAsync(It.IsAny<TransformationLog>()), Times.Once);
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
        SetupSuccessfulTransformation("tmpl-1", new List<FieldMapping> { mapping });

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

        var batchResult = await _sut.TransformBatchAsync("tmpl-1", records);

        Assert.Equal(3, batchResult.TotalRecords);
        Assert.Equal(3, batchResult.Results.Count);
        Assert.Equal(3, batchResult.SuccessCount);
        Assert.Equal(0, batchResult.ErrorCount);
    }

    [Fact]
    public async Task TransformBatchAsync_PersistsSingleSummaryLog()
    {
        var mapping = new FieldMapping
        {
            SourcePath = "id",
            TargetPath = "recordId",
            TransformationType = TransformationType.Direct
        };
        SetupSuccessfulTransformation("tmpl-1", new List<FieldMapping> { mapping });

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

        await _sut.TransformBatchAsync("tmpl-1", records);

        _logRepoMock.Verify(r => r.CreateAsync(It.IsAny<TransformationLog>()), Times.Once);
    }

    private void SetupSuccessfulTransformation(string templateId, List<FieldMapping> mappings)
    {
        _templateRepoMock
            .Setup(r => r.GetLatestVersionAsync(templateId))
            .ReturnsAsync(DefaultTemplate);
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(templateId, It.IsAny<int?>()))
            .ReturnsAsync(DefaultTemplate);
        _mappingRepoMock
            .Setup(r => r.GetByTemplateIdOrderedAsync(templateId))
            .ReturnsAsync(mappings);
    }
}
