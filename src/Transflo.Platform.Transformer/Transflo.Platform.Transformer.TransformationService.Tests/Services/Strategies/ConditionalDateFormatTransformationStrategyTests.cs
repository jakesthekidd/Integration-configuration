using Moq;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

public class ConditionalDateFormatTransformationStrategyTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly ConditionalDateFormatTransformationStrategy _sut;

    public ConditionalDateFormatTransformationStrategyTests()
    {
        _sut = new ConditionalDateFormatTransformationStrategy(_jsonParserMock.Object);
    }

    private TransformationContext MakeContext(string? config) =>
        new()
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = new FieldMapping
            {
                SourcePath = "irrelevant",
                TargetPath = "actualArrival",
                TransformationConfig = config
            }
        };

    private void SetupField(string path, string? value) =>
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), path))
            .ReturnsAsync((object?)value);

    // ── Identity ──────────────────────────────────────────────────────────────

    [Fact]
    public void TransformationType_Returns_ConditionalDateFormat()
    {
        Assert.Equal(TransformationType.ConditionalDateFormat, _sut.TransformationType);
    }

    // ── Real-world scenario: actualArrival ────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Origin_ReturnsActualPickup_WhenPresent()
    {
        SetupField("stopType", "Origin");
        SetupField("actualPickup", "2024-03-15T10:30:00Z");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin",      "SourcePaths": ["actualPickup", "pickUpBy"] },
                { "Value": "Destination", "SourcePaths": ["actualDelivery"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T10:30:00.000000Z", result);
    }

    [Fact]
    public async Task ApplyAsync_Origin_FallsBackToPickUpBy_WhenActualPickupIsNull()
    {
        SetupField("stopType", "Origin");
        SetupField("actualPickup", null);
        SetupField("pickUpBy", "2024-03-15T08:00:00Z");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin",      "SourcePaths": ["actualPickup", "pickUpBy"] },
                { "Value": "Destination", "SourcePaths": ["actualDelivery"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T08:00:00.000000Z", result);
    }

    [Fact]
    public async Task ApplyAsync_Destination_ReturnsActualDelivery()
    {
        SetupField("stopType", "Destination");
        SetupField("actualDelivery", "2024-03-16T14:00:00Z");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin",      "SourcePaths": ["actualPickup", "pickUpBy"] },
                { "Value": "Destination", "SourcePaths": ["actualDelivery"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-16T14:00:00.000000Z", result);
    }

    // ── Real-world scenario: scheduledEarlyArrival ────────────────────────────

    [Fact]
    public async Task ApplyAsync_Destination_ReturnsDeliverBy_WhenPresent()
    {
        SetupField("stopType", "Destination");
        SetupField("deliverBy", "2024-03-16T12:00:00Z");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Destination", "SourcePaths": ["deliverBy", "deliverByEnd"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-16T12:00:00.000000Z", result);
    }

    [Fact]
    public async Task ApplyAsync_Destination_FallsBackToDeliverByEnd_WhenDeliverByIsNull()
    {
        SetupField("stopType", "Destination");
        SetupField("deliverBy", null);
        SetupField("deliverByEnd", "2024-03-16T18:00:00Z");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Destination", "SourcePaths": ["deliverBy", "deliverByEnd"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-16T18:00:00.000000Z", result);
    }

    [Fact]
    public async Task ApplyAsync_Origin_ReturnsNull_WhenNoBranchMatchesForScheduled()
    {
        SetupField("stopType", "Origin");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Destination", "SourcePaths": ["deliverBy", "deliverByEnd"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    // ── UTC conversion ────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ConvertsOffsetDateTimeToUtc()
    {
        SetupField("stopType", "Origin");
        SetupField("actualPickup", "2024-03-15T12:30:00+02:00");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin", "SourcePaths": ["actualPickup"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T10:30:00.000000Z", result);
    }

    [Fact]
    public async Task ApplyAsync_ConvertsNegativeOffsetToUtc()
    {
        SetupField("stopType", "Origin");
        SetupField("actualPickup", "2024-03-15T06:00:00-05:00");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin", "SourcePaths": ["actualPickup"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T11:00:00.000000Z", result);
    }

    // ── OutputFormat ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_UsesDefaultIsoMicrosecondsFormat_WhenOutputFormatAbsent()
    {
        SetupField("stopType", "Origin");
        SetupField("actualPickup", "2024-03-15T10:30:00Z");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin", "SourcePaths": ["actualPickup"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T10:30:00.000000Z", result);
    }

    [Fact]
    public async Task ApplyAsync_UsesCustomOutputFormat_WhenSpecified()
    {
        SetupField("stopType", "Origin");
        SetupField("actualPickup", "2024-03-15T10:30:00Z");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "OutputFormat": "yyyy-MM-dd",
              "Branches": [
                { "Value": "Origin", "SourcePaths": ["actualPickup"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15", result);
    }

    [Fact]
    public async Task ApplyAsync_UsesIso8601RoundTrip_WhenOutputFormatIsO()
    {
        SetupField("stopType", "Origin");
        SetupField("actualPickup", "2024-03-15T10:30:00Z");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "OutputFormat": "o",
              "Branches": [
                { "Value": "Origin", "SourcePaths": ["actualPickup"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.NotNull(result);
        Assert.Contains("2024-03-15", result!.ToString());
    }

    // ── Branch matching ───────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_IsCaseInsensitive_ForBranchValueMatching()
    {
        SetupField("stopType", "ORIGIN");
        SetupField("actualPickup", "2024-03-15T10:30:00Z");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin", "SourcePaths": ["actualPickup"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T10:30:00.000000Z", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConditionValueMatchesNoeBranch()
    {
        SetupField("stopType", "StopOff");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin",      "SourcePaths": ["actualPickup"] },
                { "Value": "Destination", "SourcePaths": ["actualDelivery"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_UsesFirstMatchingBranch_WhenDuplicateValues()
    {
        SetupField("stopType", "Origin");
        SetupField("actualPickup", "2024-03-15T10:00:00Z");
        SetupField("otherField", "2024-03-15T12:00:00Z");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin", "SourcePaths": ["actualPickup"] },
                { "Value": "Origin", "SourcePaths": ["otherField"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T10:00:00.000000Z", result);
    }

    // ── Path coalescing ───────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_SkipsEmptyPaths_AndUsesNextNonEmpty()
    {
        SetupField("stopType", "Origin");
        SetupField("path1", null);
        SetupField("path2", "   ");
        SetupField("path3", "2024-03-15T10:30:00Z");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin", "SourcePaths": ["path1", "path2", "path3"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T10:30:00.000000Z", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenAllSourcePathsAreEmpty()
    {
        SetupField("stopType", "Origin");
        SetupField("path1", null);
        SetupField("path2", null);

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin", "SourcePaths": ["path1", "path2"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    // ── Unparseable dates ─────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsRawString_WhenValueIsNotAValidDate()
    {
        SetupField("stopType", "Origin");
        SetupField("actualPickup", "not-a-date");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin", "SourcePaths": ["actualPickup"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("not-a-date", result);
    }

    // ── Mode 1: top-level SourcePaths coalesce ────────────────────────────────

    [Fact]
    public async Task Mode1_ReturnsFirstField_WhenItHasAValue()
    {
        SetupField("actualPickup", "2024-03-15T10:30:00Z");

        var ctx = MakeContext("""{"SourcePaths":["actualPickup","pickUpBy"]}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T10:30:00.000000Z", result);
    }

    [Fact]
    public async Task Mode1_FallsBackToSecondField_WhenFirstIsNull()
    {
        SetupField("actualPickup", null);
        SetupField("pickUpBy", "2024-03-15T08:00:00Z");

        var ctx = MakeContext("""{"SourcePaths":["actualPickup","pickUpBy"]}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T08:00:00.000000Z", result);
    }

    [Fact]
    public async Task Mode1_FallsBackToSecondField_WhenFirstIsWhitespace()
    {
        SetupField("actualPickup", "   ");
        SetupField("pickUpBy", "2024-03-15T08:00:00Z");

        var ctx = MakeContext("""{"SourcePaths":["actualPickup","pickUpBy"]}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T08:00:00.000000Z", result);
    }

    [Fact]
    public async Task Mode1_ConvertsOffsetToUtc()
    {
        SetupField("actualPickup", "2024-03-15T12:30:00+02:00");

        var ctx = MakeContext("""{"SourcePaths":["actualPickup"]}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T10:30:00.000000Z", result);
    }

    [Fact]
    public async Task Mode1_ReturnsNull_WhenAllPathsAreEmpty()
    {
        SetupField("actualPickup", null);
        SetupField("pickUpBy", null);

        var ctx = MakeContext("""{"SourcePaths":["actualPickup","pickUpBy"]}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task Mode1_UsesCustomOutputFormat()
    {
        SetupField("actualPickup", "2024-03-15T10:30:00Z");

        var ctx = MakeContext("""{"SourcePaths":["actualPickup"],"OutputFormat":"yyyy-MM-dd"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15", result);
    }

    [Fact]
    public async Task Mode1_ReturnsRawString_WhenValueIsNotAValidDate()
    {
        SetupField("actualPickup", "not-a-date");

        var ctx = MakeContext("""{"SourcePaths":["actualPickup"]}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("not-a-date", result);
    }

    [Fact]
    public async Task Mode1_TakesPrecedenceOverConditionFieldConfig_WhenBothPresent()
    {
        // SourcePaths at root → Mode 1 wins; ConditionField is ignored
        SetupField("actualPickup", "2024-03-15T10:30:00Z");

        var ctx = MakeContext("""
            {
              "SourcePaths": ["actualPickup"],
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin", "SourcePaths": ["otherField"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T10:30:00.000000Z", result);
    }

    // ── Invalid / missing config ──────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConfigIsNull()
    {
        var ctx = MakeContext(null);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConfigIsInvalidJson()
    {
        var ctx = MakeContext("not-json");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConditionFieldKeyIsMissing()
    {
        SetupField("stopType", "Origin");

        var ctx = MakeContext("""
            {
              "Branches": [
                { "Value": "Origin", "SourcePaths": ["actualPickup"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenBranchesKeyIsMissing()
    {
        SetupField("stopType", "Origin");

        var ctx = MakeContext("""{"ConditionField": "stopType"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenBranchesArrayIsEmpty()
    {
        SetupField("stopType", "Origin");

        var ctx = MakeContext("""{"ConditionField":"stopType","Branches":[]}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConditionFieldValueIsNull()
    {
        SetupField("stopType", null);

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin", "SourcePaths": ["actualPickup"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    // ── Multiple branches / three-way switch ──────────────────────────────────

    [Fact]
    public async Task ApplyAsync_SupportsThreeBranches_AndSelectsCorrectOne()
    {
        SetupField("stopType", "StopOff");
        SetupField("stopOffArrival", "2024-03-15T13:00:00Z");

        var ctx = MakeContext("""
            {
              "ConditionField": "stopType",
              "Branches": [
                { "Value": "Origin",      "SourcePaths": ["actualPickup"]  },
                { "Value": "StopOff",     "SourcePaths": ["stopOffArrival"] },
                { "Value": "Destination", "SourcePaths": ["actualDelivery"] }
              ]
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("2024-03-15T13:00:00.000000Z", result);
    }
}
