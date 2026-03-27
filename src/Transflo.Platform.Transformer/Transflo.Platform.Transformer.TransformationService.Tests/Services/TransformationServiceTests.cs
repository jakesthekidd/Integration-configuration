using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;
using TransSvc = Transflo.Platform.Transformer.TransformationService.Services;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services;

public class TransformationServiceTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly Mock<ITransformationStrategyFactory> _strategyFactoryMock = new();
    private readonly Mock<ILogger<TransSvc.TransformationService>> _loggerMock = new();
    private readonly TransSvc.TransformationService _sut;

    private static readonly FieldMappingTemplate DefaultTemplate = new()
    {
        TemplateId = new Guid("00000000-0000-0000-0000-000000000001"),
        Name = "McLeod to WFAI Transformation",
        Version = 1
    };

    // Compact McLeod TMS order JSON used across test cases
    private const string McLeodSampleJson = """
        {
          "id": "3089050",
          "status": "D",
          "pickup_date": "20260116000000-08:00",
          "order_mode": "T",
          "total_weight": 42000.5,
          "customer": {
            "id": "CUST-WFAI",
            "name": "WFAI Logistics",
            "address1": "100 Commerce Blvd",
            "city": "Atlanta",
            "state": "GA",
            "zip": "30301"
          },
          "movement": [
            {
              "movement_id": "MOV-9050-1",
              "carrier_id": "ABCD",
              "driver_first_name": "John",
              "driver_last_name": "Doe"
            }
          ],
          "stops": [
            {
              "stop_type": "PU",
              "stop_num": 1,
              "company_name": "Origin Warehouse",
              "city": "Memphis",
              "state": "TN",
              "notes": "Call ahead 30 minutes"
            },
            {
              "stop_type": "SO",
              "stop_num": 2,
              "company_name": "Destination DC",
              "city": "Dallas",
              "state": "TX",
              "notes": ""
            }
          ]
        }
        """;

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
        var mappings = new List<FieldMapping> { new() { SourcePath = "id", TargetPath = "externalId" } };

        var result = await _sut.TransformAsync("not-valid-json", DefaultTemplate, mappings);

        Assert.False(result.Success);
        Assert.Equal("TRANSFORMATION_ERROR", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenSourceJsonDeserializesToNull()
    {
        var mappings = new List<FieldMapping> { new() { SourcePath = "id", TargetPath = "externalId" } };

        var result = await _sut.TransformAsync("null", DefaultTemplate, mappings);

        Assert.False(result.Success);
        Assert.Equal("INVALID_JSON", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsSuccess_WithMappedFields()
    {
        // fm-mcleod-001: Direct copy of McLeod order ID to externalId
        var mapping = new FieldMapping
        {
            SourcePath = "id",
            TargetPath = "externalId",
            TransformationType = TransformationType.Direct
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync("3089050");
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);
        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "externalId", "3089050"))
            .Returns(Task.CompletedTask);

        var result = await _sut.TransformAsync(McLeodSampleJson, DefaultTemplate, new List<FieldMapping> { mapping });

        Assert.True(result.Success);
        Assert.Equal(1, result.FieldsMapped);
        Assert.Equal(0, result.FieldsSkipped);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task TransformAsync_ReturnsFailure_WhenRequiredFieldMissing()
    {
        // fm-mcleod-001: id is required; test with a payload that omits it
        var mapping = new FieldMapping
        {
            SourcePath = "id",
            TargetPath = "externalId",
            TransformationType = TransformationType.Direct,
            IsRequired = true
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync((object?)null);
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);

        var result = await _sut.TransformAsync("""{"status":"D"}""", DefaultTemplate, new List<FieldMapping> { mapping });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.ErrorCode == "REQUIRED_FIELD_MISSING");
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task TransformAsync_NoWarning_WhenRequiredFieldMissing()
    {
        // Required fields that fail should produce an error, never a duplicate warning
        var mapping = new FieldMapping
        {
            SourcePath = "id",
            TargetPath = "externalId",
            TransformationType = TransformationType.Direct,
            IsRequired = true
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync((object?)null);
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);

        var result = await _sut.TransformAsync("""{"status":"D"}""", DefaultTemplate, new List<FieldMapping> { mapping });

        Assert.DoesNotContain(result.Warnings, w => w.Code == "FIELD_VALUE_MISSING");
    }

    [Fact]
    public async Task TransformAsync_UsesDefaultValue_WhenStrategyReturnsNull()
    {
        // fm-mcleod-022: pro_number is optional; default "N/A" used when absent
        var mapping = new FieldMapping
        {
            SourcePath = "pro_number",
            TargetPath = "proNumber",
            TransformationType = TransformationType.Direct,
            DefaultValue = "N/A",
            IsRequired = false
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync((object?)null);
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);
        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "proNumber", "N/A"))
            .Returns(Task.CompletedTask);

        var result = await _sut.TransformAsync("""{"id":"3089050","status":"D"}""", DefaultTemplate, new List<FieldMapping> { mapping });

        Assert.True(result.Success);
        Assert.Equal(1, result.FieldsMapped);
        _jsonParserMock.Verify(
            p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "proNumber", "N/A"),
            Times.Once);
    }

    [Fact]
    public async Task TransformAsync_FallsBackToDirect_WhenStrategyNotFound()
    {
        // fm-mcleod-112: Math type is not implemented; falls back to direct copy with a warning
        var mapping = new FieldMapping
        {
            SourcePath = "total_weight",
            TargetPath = "totalWeight",
            TransformationType = TransformationType.Math
        };

        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Math))
            .Returns((ITransformationStrategy?)null);
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "total_weight"))
            .ReturnsAsync("42000.5");
        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "totalWeight", "42000.5"))
            .Returns(Task.CompletedTask);

        var result = await _sut.TransformAsync(McLeodSampleJson, DefaultTemplate, new List<FieldMapping> { mapping });

        Assert.True(result.Success);
        Assert.Single(result.Warnings);
        Assert.Equal("UNSUPPORTED_TRANSFORMATION_TYPE", result.Warnings[0].Code);
        Assert.Equal(1, result.FieldsMapped);
    }

    [Fact]
    public async Task TransformAsync_AddsWarning_WhenOptionalFieldMissing()
    {
        // fm-mcleod-022: pro_number is optional; warn but do not fail when absent
        var mapping = new FieldMapping
        {
            SourcePath = "pro_number",
            TargetPath = "proNumber",
            TransformationType = TransformationType.Direct,
            IsRequired = false
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync((object?)null);
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);

        var result = await _sut.TransformAsync("""{"id":"3089050","status":"D"}""", DefaultTemplate, new List<FieldMapping> { mapping });

        Assert.True(result.Success);
        Assert.Equal(0, result.FieldsMapped);
        Assert.Equal(1, result.FieldsSkipped);
        Assert.Contains(result.Warnings, w => w.Code == "FIELD_VALUE_MISSING");
    }

    [Fact]
    public async Task TransformAsync_NoWarning_ForConstantType_WhenValueIsNull()
    {
        // fm-mcleod-038: customer.country is a Constant – no source path, null result is expected
        var mapping = new FieldMapping
        {
            SourcePath = "",
            TargetPath = "customer.country",
            TransformationType = TransformationType.Constant,
            DefaultValue = null
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync((object?)null);
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Constant))
            .Returns(strategyMock.Object);

        var result = await _sut.TransformAsync(McLeodSampleJson, DefaultTemplate, new List<FieldMapping> { mapping });

        Assert.DoesNotContain(result.Warnings, w => w.Code == "FIELD_VALUE_MISSING");
    }

    [Fact]
    public async Task TransformBatchAsync_ProcessesAllRecords()
    {
        // fm-mcleod-001: Direct copy of id to externalId across multiple McLeod orders
        var mapping = new FieldMapping
        {
            SourcePath = "id",
            TargetPath = "externalId",
            TransformationType = TransformationType.Direct
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync("some-id");
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);
        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "externalId", "some-id"))
            .Returns(Task.CompletedTask);

        var records = new List<JsonElement>
        {
            JsonDocument.Parse("""{"id":"3089050"}""").RootElement,
            JsonDocument.Parse("""{"id":"3089051"}""").RootElement,
            JsonDocument.Parse("""{"id":"3089052"}""").RootElement
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
        // Log persistence is the coordinator's responsibility, not the service's
        var mapping = new FieldMapping
        {
            SourcePath = "id",
            TargetPath = "externalId",
            TransformationType = TransformationType.Direct
        };

        var strategyMock = new Mock<ITransformationStrategy>();
        strategyMock.Setup(s => s.ApplyAsync(It.IsAny<TransformationContext>())).ReturnsAsync("val");
        _strategyFactoryMock
            .Setup(f => f.GetStrategy(TransformationType.Direct))
            .Returns(strategyMock.Object);
        _jsonParserMock
            .Setup(p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "externalId", "val"))
            .Returns(Task.CompletedTask);

        var records = new List<JsonElement>
        {
            JsonDocument.Parse("""{"id":"3089050"}""").RootElement,
            JsonDocument.Parse("""{"id":"3089051"}""").RootElement
        };

        await _sut.TransformBatchAsync(DefaultTemplate, new List<FieldMapping> { mapping }, records);

        _jsonParserMock.Verify(
            p => p.SetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), "externalId", "val"),
            Times.Exactly(2));
    }
}
