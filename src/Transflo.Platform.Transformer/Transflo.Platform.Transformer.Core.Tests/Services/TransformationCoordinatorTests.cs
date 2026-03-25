using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;
using Transflo.Platform.Transformer.Core.Services;
using Transflo.Platform.Transformer.TransformationService.DTOs;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using ServiceModels = Transflo.Platform.Transformer.TransformationService.Models;

namespace Transflo.Platform.Transformer.Core.Tests.Services;

public class TransformationCoordinatorTests
{
    private readonly Mock<ITemplateRepository> _templateRepoMock = new();
    private readonly Mock<IFieldMappingRepository> _mappingRepoMock = new();
    private readonly Mock<ITransformationLogRepository> _logRepoMock = new();
    private readonly Mock<ITransformationService> _serviceMock = new();
    private readonly Mock<ITemplateVersionRepository> _versionRepoMock = new();
    private readonly Mock<ILogger<TransformationCoordinator>> _loggerMock = new();
    private readonly TransformationCoordinator _sut;

    private static readonly Template DefaultEfTemplate = new()
    {
        Id = new Guid("e3a7f2c1-9b4d-4e8a-8f6c-2d1a5b0c9e37"),
        Name = "McLeod to WFAI Transformation"
    };

    private static readonly TemplateVersion DefaultEfVersion = new()
    {
        Id = new Guid("e3a7f2c1-9b4d-4e8a-8f6c-2d1a5b0c9e37"),
        TemplateId = DefaultEfTemplate.Id,
        Version = 1,
        Status = TemplateVersionStatus.Published
    };

    private static readonly List<FieldMapping> DefaultEfMappings =
    [
        new() { SourcePath = "id",                          TargetPath = "externalId",          ExecutionOrder = 1 },
        new() { SourcePath = "status",                      TargetPath = "status",               ExecutionOrder = 2 },
        new() { SourcePath = "movement[0].override_pay_amt",TargetPath = "totalAmount",          ExecutionOrder = 3 },
        new() { SourcePath = "blnum",                       TargetPath = "bolNumber",            ExecutionOrder = 4 },
        new() { SourcePath = "customer.id",                 TargetPath = "portalCustomerId",     ExecutionOrder = 5 },
        new() { SourcePath = "stops[0].location_name",      TargetPath = "stops[0].name",        ExecutionOrder = 6 },
        new() { SourcePath = "stops[0].stop_type",          TargetPath = "stops[0].type",        ExecutionOrder = 7 },
        new() { SourcePath = "stops[1].location_name",      TargetPath = "stops[1].name",        ExecutionOrder = 8 },
        new() { SourcePath = "stops[1].stop_type",          TargetPath = "stops[1].type",        ExecutionOrder = 9 },
    ];

    // Realistic McLeod TMS order payload (trimmed to fields relevant to the mapping above)
    private const string SampleMcLeodOrderJson = """
        {
          "__type": "orders",
          "id": "3089050",
          "status": "D",
          "blnum": "1021983029",
          "customer_id": "CFAAPAWA",
          "commodity": "RETAIL PRODUCTS",
          "freight_charge": 576.24,
          "weight": 14034.8,
          "weight_um": "LB",
          "ordered_date": "20260114102400-0800",
          "stops": [
            {
              "__type": "stop",
              "id": "zz1jeus0s3t0ulsCFAATS2",
              "stop_type": "PU",
              "location_id": "WHIRSPWA",
              "location_name": "WHIRLPOOL",
              "address": "19700 38TH AVE E",
              "city_name": "SPANAWAY",
              "state": "WA",
              "zip_code": "98387",
              "latitude": 47.0797,
              "longitude": -122.2922,
              "sched_arrive_early": "20260115060000-0800",
              "sched_arrive_late": "20260115060000-0800",
              "actual_arrival": "20260115060000-0800",
              "actual_departure": "20260115070000-0800",
              "movement_id": "1281956"
            },
            {
              "__type": "stop",
              "id": "zz1jeus0sco0ulsCFAATS2",
              "stop_type": "SO",
              "location_id": "MODPPOOR",
              "location_name": "MOD PORTLAND WAREHOUSE",
              "address": "15450 NE AIRPORT WAY",
              "city_name": "PORTLAND",
              "state": "OR",
              "zip_code": "97230",
              "latitude": 45.5336,
              "longitude": -122.4767,
              "sched_arrive_early": "20260115090000-0800",
              "sched_arrive_late": "20260115090000-0800",
              "actual_arrival": "20260115084000-0800",
              "actual_departure": "20260115095500-0800",
              "movement_id": "1281956"
            }
          ],
          "movement": [
            {
              "__type": "movement",
              "id": "1281956",
              "status": "D",
              "override_pay_amt": 550.0,
              "override_payee_id": "RTCPAWA"
            }
          ],
          "customer": {
            "__type": "customer",
            "id": "CFAAPAWA",
            "name": "CHEEMA FREIGHTLINES"
          }
        }
        """;

    // Second order used in batch tests – different order ID and in-transit status
    private const string SampleMcLeodOrder2Json = """
        {
          "__type": "orders",
          "id": "3089051",
          "status": "A",
          "blnum": "1021983030",
          "customer_id": "CFAAPAWA",
          "commodity": "RETAIL PRODUCTS",
          "freight_charge": 620.00,
          "weight": 12500.0,
          "weight_um": "LB",
          "ordered_date": "20260114120000-0800",
          "stops": [
            {
              "__type": "stop",
              "id": "zz1jeus0s3t0uls2CFAATS2",
              "stop_type": "PU",
              "location_id": "WHIRSPWA",
              "location_name": "WHIRLPOOL",
              "address": "19700 38TH AVE E",
              "city_name": "SPANAWAY",
              "state": "WA",
              "zip_code": "98387",
              "latitude": 47.0797,
              "longitude": -122.2922,
              "movement_id": "1281957"
            },
            {
              "__type": "stop",
              "id": "zz1jeus0sco0uls2CFAATS2",
              "stop_type": "SO",
              "location_id": "MODPPOOR",
              "location_name": "MOD PORTLAND WAREHOUSE",
              "address": "15450 NE AIRPORT WAY",
              "city_name": "PORTLAND",
              "state": "OR",
              "zip_code": "97230",
              "latitude": 45.5336,
              "longitude": -122.4767,
              "movement_id": "1281957"
            }
          ],
          "movement": [
            {
              "__type": "movement",
              "id": "1281957",
              "status": "A",
              "override_pay_amt": 580.0,
              "override_payee_id": "RTCPAWA"
            }
          ],
          "customer": {
            "__type": "customer",
            "id": "CFAAPAWA",
            "name": "CHEEMA FREIGHTLINES"
          }
        }
        """;

    public TransformationCoordinatorTests()
    {
        _sut = new TransformationCoordinator(
            _templateRepoMock.Object,
            _mappingRepoMock.Object,
            _logRepoMock.Object,
            _serviceMock.Object,
            _versionRepoMock.Object,
            _loggerMock.Object);

        _logRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<TransformationLog>()))
            .ReturnsAsync((TransformationLog log) => log);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenTemplateNotFound()
    {
        var templateId = new Guid("f7b8d925-3c1e-4a69-b0d4-8e2f6c1a7d53");
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(templateId))
            .ReturnsAsync((Template?)null);

        var result = await _sut.TransformAsync(SampleMcLeodOrderJson, templateId);

        Assert.False(result.Success);
        Assert.Single(result.Errors);
        Assert.Equal("TEMPLATE_NOT_FOUND", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenSpecificVersionNotFound()
    {
        var templateId = new Guid("e3a7f2c1-9b4d-4e8a-8f6c-2d1a5b0c9e37");
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(templateId))
            .ReturnsAsync(DefaultEfTemplate);
        _versionRepoMock
            .Setup(r => r.GetByVersionAsync(templateId, 99))
            .ReturnsAsync((TemplateVersion?)null);

        var result = await _sut.TransformAsync(SampleMcLeodOrderJson, templateId, version: 99);

        Assert.False(result.Success);
        Assert.Equal("TEMPLATE_NOT_FOUND", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_ReturnsError_WhenNoMappingsFound()
    {
        var templateId = new Guid("e3a7f2c1-9b4d-4e8a-8f6c-2d1a5b0c9e37");
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(templateId))
            .ReturnsAsync(DefaultEfTemplate);
        _versionRepoMock
            .Setup(r => r.GetPublishedVersionAsync(templateId))
            .ReturnsAsync(DefaultEfVersion);
        _mappingRepoMock
            .Setup(r => r.GetByTemplateVersionIdOrderedAsync(DefaultEfVersion.Id))
            .ReturnsAsync(new List<FieldMapping>());

        var result = await _sut.TransformAsync(SampleMcLeodOrderJson, templateId);

        Assert.False(result.Success);
        Assert.Equal("NO_MAPPINGS", result.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task TransformAsync_DelegatesTo_TransformationService()
    {
        var templateId = new Guid("e3a7f2c1-9b4d-4e8a-8f6c-2d1a5b0c9e37");
        SetupResolve(templateId);

        var expectedOutput = new Dictionary<string, object>
        {
            ["externalId"] = "3089050",
            ["status"] = "D",
            ["totalAmount"] = 550.0,
            ["bolNumber"] = "1021983029",
            ["portalCustomerId"] = "CFAAPAWA"
        };
        var serviceResult = new TransformationResult
        {
            Success = true,
            FieldsMapped = DefaultEfMappings.Count,
            OutputData = expectedOutput
        };
        _serviceMock
            .Setup(s => s.TransformAsync(
                SampleMcLeodOrderJson,
                It.IsAny<ServiceModels.FieldMappingTemplate>(),
                It.IsAny<List<ServiceModels.FieldMapping>>()))
            .ReturnsAsync(serviceResult);

        var result = await _sut.TransformAsync(SampleMcLeodOrderJson, templateId);

        Assert.True(result.Success);
        Assert.Equal(DefaultEfMappings.Count, result.FieldsMapped);
    }

    [Fact]
    public async Task TransformAsync_PersistsLog()
    {
        var templateId = new Guid("e3a7f2c1-9b4d-4e8a-8f6c-2d1a5b0c9e37");
        SetupResolve(templateId);
        _serviceMock
            .Setup(s => s.TransformAsync(
                It.IsAny<string>(),
                It.IsAny<ServiceModels.FieldMappingTemplate>(),
                It.IsAny<List<ServiceModels.FieldMapping>>()))
            .ReturnsAsync(new TransformationResult { Success = true, FieldsMapped = DefaultEfMappings.Count });

        await _sut.TransformAsync(SampleMcLeodOrderJson, templateId);

        _logRepoMock.Verify(r => r.CreateAsync(It.IsAny<TransformationLog>()), Times.Once);
    }

    [Fact]
    public async Task PreviewTransformationAsync_DoesNotPersistLog()
    {
        var templateId = new Guid("e3a7f2c1-9b4d-4e8a-8f6c-2d1a5b0c9e37");
        SetupResolve(templateId);
        _serviceMock
            .Setup(s => s.TransformAsync(
                It.IsAny<string>(),
                It.IsAny<ServiceModels.FieldMappingTemplate>(),
                It.IsAny<List<ServiceModels.FieldMapping>>()))
            .ReturnsAsync(new TransformationResult { Success = true, FieldsMapped = DefaultEfMappings.Count });

        await _sut.PreviewTransformationAsync(SampleMcLeodOrderJson, templateId);

        _logRepoMock.Verify(r => r.CreateAsync(It.IsAny<TransformationLog>()), Times.Never);
    }

    [Fact]
    public async Task TransformBatchAsync_ReturnsError_WhenTemplateNotFound()
    {
        var templateId = new Guid("f7b8d925-3c1e-4a69-b0d4-8e2f6c1a7d53");
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(templateId))
            .ReturnsAsync((Template?)null);

        var records = new List<JsonElement>
        {
            JsonDocument.Parse(SampleMcLeodOrderJson).RootElement,
            JsonDocument.Parse(SampleMcLeodOrder2Json).RootElement
        };

        var result = await _sut.TransformBatchAsync(templateId, records);

        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(2, result.ErrorCount);
        Assert.All(result.Results, r => Assert.False(r.Success));
    }

    [Fact]
    public async Task TransformBatchAsync_PersistsSingleSummaryLog()
    {
        var templateId = new Guid("e3a7f2c1-9b4d-4e8a-8f6c-2d1a5b0c9e37");
        SetupResolve(templateId);

        var batchResult = new BatchTransformResult
        {
            TemplateId = templateId,
            TotalRecords = 2,
            SuccessCount = 2,
            Results = new List<BatchRecordResult>
            {
                new()
                {
                    Index = 0,
                    Success = true,
                    FieldsMapped = DefaultEfMappings.Count,
                    OutputData = new Dictionary<string, object>
                    {
                        ["externalId"] = "3089050",
                        ["status"] = "D",
                        ["totalAmount"] = 550.0
                    }
                },
                new()
                {
                    Index = 1,
                    Success = true,
                    FieldsMapped = DefaultEfMappings.Count,
                    OutputData = new Dictionary<string, object>
                    {
                        ["externalId"] = "3089051",
                        ["status"] = "A",
                        ["totalAmount"] = 580.0
                    }
                }
            }
        };
        _serviceMock
            .Setup(s => s.TransformBatchAsync(
                It.IsAny<ServiceModels.FieldMappingTemplate>(),
                It.IsAny<List<ServiceModels.FieldMapping>>(),
                It.IsAny<List<JsonElement>>()))
            .ReturnsAsync(batchResult);

        var records = new List<JsonElement>
        {
            JsonDocument.Parse(SampleMcLeodOrderJson).RootElement,
            JsonDocument.Parse(SampleMcLeodOrder2Json).RootElement
        };

        await _sut.TransformBatchAsync(new Guid("e3a7f2c1-9b4d-4e8a-8f6c-2d1a5b0c9e37"), records);

        _logRepoMock.Verify(r => r.CreateAsync(It.IsAny<TransformationLog>()), Times.Once);
    }

    private void SetupResolve(Guid templateId)
    {
        _templateRepoMock
            .Setup(r => r.GetByIdAsync(templateId))
            .ReturnsAsync(DefaultEfTemplate);
        _versionRepoMock
            .Setup(r => r.GetPublishedVersionAsync(templateId))
            .ReturnsAsync(DefaultEfVersion);
        _mappingRepoMock
            .Setup(r => r.GetByTemplateVersionIdOrderedAsync(DefaultEfVersion.Id))
            .ReturnsAsync(DefaultEfMappings);
    }
}