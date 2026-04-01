using Moq;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

public class ConditionalTransformationStrategyTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly ConditionalTransformationStrategy _sut;

    public ConditionalTransformationStrategyTests()
    {
        _sut = new ConditionalTransformationStrategy(_jsonParserMock.Object);
    }

    private TransformationContext MakeContext(string config, string field = "status") =>
        new()
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = new FieldMapping
            {
                SourcePath = field,
                TargetPath = "output",
                TransformationConfig = config
            }
        };

    private void SetupField(string path, string? value) =>
        _jsonParserMock
            .Setup(p => p.GetValueAtPathAsync(It.IsAny<Dictionary<string, object>>(), path))
            .ReturnsAsync((object?)value);

    // ── Identity ──────────────────────────────────────────────────────────────

    [Fact]
    public void TransformationType_Returns_Conditional()
    {
        Assert.Equal(TransformationType.Conditional, _sut.TransformationType);
    }

    // ── Null / invalid config ─────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConfigIsNull()
    {
        var ctx = MakeContext(null!);
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
    public async Task ApplyAsync_ReturnsNull_WhenConditionsKeyMissing()
    {
        var ctx = MakeContext("""{"TrueValue":"yes","FalseValue":"no"}""");
        var result = await _sut.ApplyAsync(ctx);
        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConditionsArrayIsEmpty()
    {
        var ctx = MakeContext("""{"Conditions":[],"TrueValue":"yes"}""");
        var result = await _sut.ApplyAsync(ctx);
        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConditionMissingFieldProperty()
    {
        SetupField("status", "ACTIVE");
        var ctx = MakeContext("""{"Conditions":[{"Operator":"equals","Value":"ACTIVE"}],"TrueValue":"yes"}""");
        var result = await _sut.ApplyAsync(ctx);
        // Condition evaluation returns false, FalseValue is absent
        Assert.Null(result);
    }

    // ── equals ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenEqualsConditionMatches()
    {
        SetupField("status", "ACTIVE");
        var ctx = MakeContext("""{"Conditions":[{"Field":"status","Operator":"equals","Value":"ACTIVE"}],"TrueValue":"Active","FalseValue":"Inactive"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Active", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsFalseValue_WhenEqualsConditionDoesNotMatch()
    {
        SetupField("status", "INACTIVE");
        var ctx = MakeContext("""{"Conditions":[{"Field":"status","Operator":"equals","Value":"ACTIVE"}],"TrueValue":"Active","FalseValue":"Inactive"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Inactive", result);
    }

    [Fact]
    public async Task ApplyAsync_EqualsIsCaseInsensitive()
    {
        SetupField("status", "active");
        var ctx = MakeContext("""{"Conditions":[{"Field":"status","Operator":"equals","Value":"ACTIVE"}],"TrueValue":"yes"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("yes", result);
    }

    // ── notequals ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenNotEqualsConditionMatches()
    {
        SetupField("status", "INACTIVE");
        var ctx = MakeContext("""{"Conditions":[{"Field":"status","Operator":"notequals","Value":"ACTIVE"}],"TrueValue":"Not active","FalseValue":"Active"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Not active", result);
    }

    // ── contains / startswith / endswith ──────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenContainsConditionMatches()
    {
        SetupField("description", "Full Truckload shipment");
        var ctx = MakeContext("""{"Conditions":[{"Field":"description","Operator":"contains","Value":"Truckload"}],"TrueValue":"TL"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("TL", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenStartsWithConditionMatches()
    {
        SetupField("code", "TL-001");
        var ctx = MakeContext("""{"Conditions":[{"Field":"code","Operator":"startswith","Value":"TL"}],"TrueValue":"Truckload"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Truckload", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenEndsWithConditionMatches()
    {
        SetupField("code", "ORDER-FINAL");
        var ctx = MakeContext("""{"Conditions":[{"Field":"code","Operator":"endswith","Value":"FINAL"}],"TrueValue":"Completed"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Completed", result);
    }

    // ── numeric comparisons ────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenGreaterThanConditionMatches()
    {
        SetupField("weight", "150");
        var ctx = MakeContext("""{"Conditions":[{"Field":"weight","Operator":"greaterthan","Value":"100"}],"TrueValue":"Heavy","FalseValue":"Light"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Heavy", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsFalseValue_WhenGreaterThanConditionDoesNotMatch()
    {
        SetupField("weight", "50");
        var ctx = MakeContext("""{"Conditions":[{"Field":"weight","Operator":"greaterthan","Value":"100"}],"TrueValue":"Heavy","FalseValue":"Light"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Light", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenLessThanConditionMatches()
    {
        SetupField("quantity", "5");
        var ctx = MakeContext("""{"Conditions":[{"Field":"quantity","Operator":"lessthan","Value":"10"}],"TrueValue":"Low","FalseValue":"OK"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Low", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenGreaterThanOrEqualsConditionMatchesExact()
    {
        SetupField("score", "100");
        var ctx = MakeContext("""{"Conditions":[{"Field":"score","Operator":"greaterthanorequals","Value":"100"}],"TrueValue":"Pass"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Pass", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenLessThanOrEqualsConditionMatchesExact()
    {
        SetupField("score", "0");
        var ctx = MakeContext("""{"Conditions":[{"Field":"score","Operator":"lessthanorequals","Value":"0"}],"TrueValue":"Zero"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Zero", result);
    }

    // ── isempty / isnotempty ──────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenIsEmptyConditionMatchesNullField()
    {
        SetupField("notes", null);
        var ctx = MakeContext("""{"Conditions":[{"Field":"notes","Operator":"isempty"}],"TrueValue":"N/A","FalseValue":"Has notes"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("N/A", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenIsEmptyConditionMatchesEmptyString()
    {
        SetupField("notes", "");
        var ctx = MakeContext("""{"Conditions":[{"Field":"notes","Operator":"isempty"}],"TrueValue":"N/A"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("N/A", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenIsNotEmptyConditionMatchesNonEmptyField()
    {
        SetupField("reference", "REF-001");
        var ctx = MakeContext("""{"Conditions":[{"Field":"reference","Operator":"isnotempty"}],"TrueValue":"Has ref","FalseValue":"No ref"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Has ref", result);
    }

    // ── in / notin ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenInConditionMatchesOneOfList()
    {
        SetupField("mode", "LTL");
        var ctx = MakeContext("""{"Conditions":[{"Field":"mode","Operator":"in","Value":"TL,LTL,PTL"}],"TrueValue":"Ground","FalseValue":"Other"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Ground", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsFalseValue_WhenInConditionDoesNotMatch()
    {
        SetupField("mode", "AIR");
        var ctx = MakeContext("""{"Conditions":[{"Field":"mode","Operator":"in","Value":"TL,LTL,PTL"}],"TrueValue":"Ground","FalseValue":"Other"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Other", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenNotInConditionMatchesExclusion()
    {
        SetupField("status", "CANCELLED");
        var ctx = MakeContext("""{"Conditions":[{"Field":"status","Operator":"notin","Value":"ACTIVE,PENDING"}],"TrueValue":"Archived"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Archived", result);
    }

    // ── AND / OR logic ────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenAllConditionsMatchAndLogicIsAnd()
    {
        SetupField("status", "ACTIVE");
        SetupField("type", "TL");
        var ctx = MakeContext("""
            {
              "Conditions": [
                {"Field":"status","Operator":"equals","Value":"ACTIVE"},
                {"Field":"type","Operator":"equals","Value":"TL"}
              ],
              "ConditionLogic": "AND",
              "TrueValue": "ActiveTL",
              "FalseValue": "Other"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("ActiveTL", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsFalseValue_WhenOneConditionFailsAndLogicIsAnd()
    {
        SetupField("status", "ACTIVE");
        SetupField("type", "LTL");
        var ctx = MakeContext("""
            {
              "Conditions": [
                {"Field":"status","Operator":"equals","Value":"ACTIVE"},
                {"Field":"type","Operator":"equals","Value":"TL"}
              ],
              "ConditionLogic": "AND",
              "TrueValue": "ActiveTL",
              "FalseValue": "Other"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Other", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsTrueValue_WhenOneConditionMatchesAndLogicIsOr()
    {
        SetupField("status", "INACTIVE");
        SetupField("type", "TL");
        var ctx = MakeContext("""
            {
              "Conditions": [
                {"Field":"status","Operator":"equals","Value":"ACTIVE"},
                {"Field":"type","Operator":"equals","Value":"TL"}
              ],
              "ConditionLogic": "OR",
              "TrueValue": "Matched",
              "FalseValue": "None"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Matched", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsFalseValue_WhenNoConditionMatchesAndLogicIsOr()
    {
        SetupField("status", "INACTIVE");
        SetupField("type", "LTL");
        var ctx = MakeContext("""
            {
              "Conditions": [
                {"Field":"status","Operator":"equals","Value":"ACTIVE"},
                {"Field":"type","Operator":"equals","Value":"TL"}
              ],
              "ConditionLogic": "OR",
              "TrueValue": "Matched",
              "FalseValue": "None"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("None", result);
    }

    [Fact]
    public async Task ApplyAsync_DefaultsToAndLogic_WhenConditionLogicIsAbsent()
    {
        SetupField("status", "ACTIVE");
        SetupField("type", "LTL");
        var ctx = MakeContext("""
            {
              "Conditions": [
                {"Field":"status","Operator":"equals","Value":"ACTIVE"},
                {"Field":"type","Operator":"equals","Value":"TL"}
              ],
              "TrueValue": "yes",
              "FalseValue": "no"
            }
            """);

        // Without ConditionLogic, AND is the default → one failure → FalseValue
        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("no", result);
    }

    // ── Missing TrueValue / FalseValue ────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConditionPassesButTrueValueAbsent()
    {
        SetupField("status", "ACTIVE");
        var ctx = MakeContext("""{"Conditions":[{"Field":"status","Operator":"equals","Value":"ACTIVE"}],"FalseValue":"no"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConditionFailsAndFalseValueAbsent()
    {
        SetupField("status", "INACTIVE");
        var ctx = MakeContext("""{"Conditions":[{"Field":"status","Operator":"equals","Value":"ACTIVE"}],"TrueValue":"yes"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    // ── Condition groups: GroupLogic OR ───────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Groups_ReturnsTrueValue_WhenFirstGroupPassesAndGroupLogicIsOr()
    {
        // (status==ACTIVE AND mode==TL) OR (priority==HIGH)
        // First group passes → true
        SetupField("status", "ACTIVE");
        SetupField("mode", "TL");
        SetupField("priority", "LOW");

        var ctx = MakeContext("""
            {
              "ConditionGroups": [
                {
                  "Logic": "AND",
                  "Conditions": [
                    { "Field": "status",   "Operator": "equals", "Value": "ACTIVE" },
                    { "Field": "mode",     "Operator": "equals", "Value": "TL"     }
                  ]
                },
                {
                  "Logic": "AND",
                  "Conditions": [
                    { "Field": "priority", "Operator": "equals", "Value": "HIGH" }
                  ]
                }
              ],
              "GroupLogic": "OR",
              "TrueValue":  "Matched",
              "FalseValue": "Other"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Matched", result);
    }

    [Fact]
    public async Task ApplyAsync_Groups_ReturnsTrueValue_WhenSecondGroupPassesAndGroupLogicIsOr()
    {
        // (status==ACTIVE AND mode==TL) OR (priority==HIGH)
        // First group fails, second group passes → true
        SetupField("status", "INACTIVE");
        SetupField("mode", "LTL");
        SetupField("priority", "HIGH");

        var ctx = MakeContext("""
            {
              "ConditionGroups": [
                {
                  "Logic": "AND",
                  "Conditions": [
                    { "Field": "status", "Operator": "equals", "Value": "ACTIVE" },
                    { "Field": "mode",   "Operator": "equals", "Value": "TL"     }
                  ]
                },
                {
                  "Logic": "AND",
                  "Conditions": [
                    { "Field": "priority", "Operator": "equals", "Value": "HIGH" }
                  ]
                }
              ],
              "GroupLogic": "OR",
              "TrueValue":  "Matched",
              "FalseValue": "Other"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Matched", result);
    }

    [Fact]
    public async Task ApplyAsync_Groups_ReturnsFalseValue_WhenNoGroupPassesAndGroupLogicIsOr()
    {
        // Both groups fail → false
        SetupField("status", "INACTIVE");
        SetupField("mode", "LTL");
        SetupField("priority", "LOW");

        var ctx = MakeContext("""
            {
              "ConditionGroups": [
                {
                  "Logic": "AND",
                  "Conditions": [
                    { "Field": "status", "Operator": "equals", "Value": "ACTIVE" },
                    { "Field": "mode",   "Operator": "equals", "Value": "TL"     }
                  ]
                },
                {
                  "Logic": "AND",
                  "Conditions": [
                    { "Field": "priority", "Operator": "equals", "Value": "HIGH" }
                  ]
                }
              ],
              "GroupLogic": "OR",
              "TrueValue":  "Matched",
              "FalseValue": "Other"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Other", result);
    }

    // ── Condition groups: GroupLogic AND ──────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Groups_ReturnsTrueValue_WhenAllGroupsPassAndGroupLogicIsAnd()
    {
        // (status==ACTIVE OR mode==TL) AND (weight > 100)
        // First group: status fails, mode passes → OR → true
        // Second group: weight 150 > 100 → true
        // AND → true
        SetupField("status", "INACTIVE");
        SetupField("mode", "TL");
        SetupField("weight", "150");

        var ctx = MakeContext("""
            {
              "ConditionGroups": [
                {
                  "Logic": "OR",
                  "Conditions": [
                    { "Field": "status", "Operator": "equals",      "Value": "ACTIVE" },
                    { "Field": "mode",   "Operator": "equals",      "Value": "TL"     }
                  ]
                },
                {
                  "Logic": "AND",
                  "Conditions": [
                    { "Field": "weight", "Operator": "greaterthan", "Value": "100" }
                  ]
                }
              ],
              "GroupLogic": "AND",
              "TrueValue":  "Qualified",
              "FalseValue": "Rejected"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Qualified", result);
    }

    [Fact]
    public async Task ApplyAsync_Groups_ReturnsFalseValue_WhenOneGroupFailsAndGroupLogicIsAnd()
    {
        // (status==ACTIVE OR mode==TL) AND (weight > 100)
        // First group: both fail → OR → false
        // AND short-circuits → false
        SetupField("status", "INACTIVE");
        SetupField("mode", "LTL");
        SetupField("weight", "150");

        var ctx = MakeContext("""
            {
              "ConditionGroups": [
                {
                  "Logic": "OR",
                  "Conditions": [
                    { "Field": "status", "Operator": "equals",      "Value": "ACTIVE" },
                    { "Field": "mode",   "Operator": "equals",      "Value": "TL"     }
                  ]
                },
                {
                  "Logic": "AND",
                  "Conditions": [
                    { "Field": "weight", "Operator": "greaterthan", "Value": "100" }
                  ]
                }
              ],
              "GroupLogic": "AND",
              "TrueValue":  "Qualified",
              "FalseValue": "Rejected"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Rejected", result);
    }

    // ── Condition groups: inner OR logic ──────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Groups_ReturnsTrueValue_WhenInnerOrGroupPartiallyMatches()
    {
        // One group with OR: stop_type is PU or SO
        SetupField("stop_type", "SO");

        var ctx = MakeContext("""
            {
              "ConditionGroups": [
                {
                  "Logic": "OR",
                  "Conditions": [
                    { "Field": "stop_type", "Operator": "equals", "Value": "PU" },
                    { "Field": "stop_type", "Operator": "equals", "Value": "SO" }
                  ]
                }
              ],
              "GroupLogic": "AND",
              "TrueValue":  "Valid Stop",
              "FalseValue": "Invalid Stop"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Valid Stop", result);
    }

    // ── Groups: invalid / edge cases ──────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_Groups_ReturnsNull_WhenConditionGroupsIsEmpty()
    {
        var ctx = MakeContext("""{"ConditionGroups":[],"TrueValue":"yes"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_Groups_DefaultsToAndGroupLogic_WhenGroupLogicIsAbsent()
    {
        // Both groups must pass (default AND); second group fails → FalseValue
        SetupField("status", "ACTIVE");
        SetupField("mode", "LTL");

        var ctx = MakeContext("""
            {
              "ConditionGroups": [
                {
                  "Logic": "AND",
                  "Conditions": [{ "Field": "status", "Operator": "equals", "Value": "ACTIVE" }]
                },
                {
                  "Logic": "AND",
                  "Conditions": [{ "Field": "mode",   "Operator": "equals", "Value": "TL" }]
                }
              ],
              "TrueValue":  "yes",
              "FalseValue": "no"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("no", result);
    }

    [Fact]
    public async Task ApplyAsync_Groups_TakesPrecedenceOverFlatConditions_WhenBothPresent()
    {
        // ConditionGroups present and passing → uses group evaluation, not flat
        SetupField("status", "ACTIVE");
        SetupField("mode", "LTL"); // flat Conditions would fail on this

        var ctx = MakeContext("""
            {
              "ConditionGroups": [
                {
                  "Logic": "AND",
                  "Conditions": [{ "Field": "status", "Operator": "equals", "Value": "ACTIVE" }]
                }
              ],
              "GroupLogic": "OR",
              "Conditions": [
                { "Field": "mode", "Operator": "equals", "Value": "TL" }
              ],
              "ConditionLogic": "AND",
              "TrueValue":  "GroupWon",
              "FalseValue": "FlatWon"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("GroupWon", result);
    }

    // ── MapSourceOnTrue flag ──────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsSourceFieldValue_WhenMapSourceOnTrueAndConditionPasses()
    {
        SetupField("status", "ACTIVE");
        SetupField("reference", "REF-001");

        var ctx = new TransformationContext
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = new FieldMapping
            {
                SourcePath = "reference",
                TargetPath = "output",
                TransformationConfig = """
                    {
                      "Conditions": [{ "Field": "status", "Operator": "equals", "Value": "ACTIVE" }],
                      "MapSourceOnTrue": true,
                      "FalseValue": "N/A"
                    }
                    """
            }
        };

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("REF-001", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsFalseValue_WhenMapSourceOnTrueButConditionFails()
    {
        SetupField("status", "INACTIVE");
        SetupField("reference", "REF-001");

        var ctx = new TransformationContext
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = new FieldMapping
            {
                SourcePath = "reference",
                TargetPath = "output",
                TransformationConfig = """
                    {
                      "Conditions": [{ "Field": "status", "Operator": "equals", "Value": "ACTIVE" }],
                      "MapSourceOnTrue": true,
                      "FalseValue": "N/A"
                    }
                    """
            }
        };

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("N/A", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsFalsePath_WhenMapSourceOnTrueButConditionFails()
    {
        SetupField("status", "INACTIVE");
        SetupField("reference", "REF-001");
        SetupField("fallback.ref", "FB-999");

        var ctx = new TransformationContext
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = new FieldMapping
            {
                SourcePath = "reference",
                TargetPath = "output",
                TransformationConfig = """
                    {
                      "Conditions": [{ "Field": "status", "Operator": "equals", "Value": "ACTIVE" }],
                      "MapSourceOnTrue": true,
                      "FalsePath": "fallback.ref"
                    }
                    """
            }
        };

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("FB-999", result);
    }

    [Fact]
    public async Task ApplyAsync_IgnoresMapSourceOnTrue_WhenFlagIsFalse()
    {
        SetupField("status", "ACTIVE");

        var ctx = new TransformationContext
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = new FieldMapping
            {
                SourcePath = "reference",
                TargetPath = "output",
                TransformationConfig = """
                    {
                      "Conditions": [{ "Field": "status", "Operator": "equals", "Value": "ACTIVE" }],
                      "MapSourceOnTrue": false,
                      "TrueValue": "Literal"
                    }
                    """
            }
        };

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Literal", result);
    }

    [Fact]
    public async Task ApplyAsync_MapSourceOnTrue_WorksWithConditionGroups()
    {
        SetupField("status", "ACTIVE");
        SetupField("mode", "TL");
        SetupField("pro_number", "PRO-456");

        var ctx = new TransformationContext
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = new FieldMapping
            {
                SourcePath = "pro_number",
                TargetPath = "output",
                TransformationConfig = """
                    {
                      "ConditionGroups": [
                        {
                          "Logic": "AND",
                          "Conditions": [
                            { "Field": "status", "Operator": "equals", "Value": "ACTIVE" },
                            { "Field": "mode",   "Operator": "equals", "Value": "TL"     }
                          ]
                        }
                      ],
                      "GroupLogic": "AND",
                      "MapSourceOnTrue": true,
                      "FalseValue": "N/A"
                    }
                    """
            }
        };

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("PRO-456", result);
    }

    // ── Path-based output: TruePath / FalsePath ───────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsTruePathValue_WhenConditionPassesAndTruePathConfigured()
    {
        SetupField("status", "ACTIVE");
        SetupField("approved.label", "Approved");

        var ctx = MakeContext("""
            {
              "Conditions": [{ "Field": "status", "Operator": "equals", "Value": "ACTIVE" }],
              "TruePath":  "approved.label",
              "FalseValue": "Rejected"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Approved", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsFalsePathValue_WhenConditionFailsAndFalsePathConfigured()
    {
        SetupField("status", "INACTIVE");
        SetupField("fallback.label", "Rejected");

        var ctx = MakeContext("""
            {
              "Conditions": [{ "Field": "status", "Operator": "equals", "Value": "ACTIVE" }],
              "TrueValue": "Approved",
              "FalsePath": "fallback.label"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Rejected", result);
    }

    [Fact]
    public async Task ApplyAsync_TruePath_TakesPrecedenceOverTrueValue_WhenBothConfigured()
    {
        SetupField("status", "ACTIVE");
        SetupField("dynamic.label", "Dynamic Result");

        var ctx = MakeContext("""
            {
              "Conditions": [{ "Field": "status", "Operator": "equals", "Value": "ACTIVE" }],
              "TruePath":  "dynamic.label",
              "TrueValue": "Static Result"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Dynamic Result", result);
    }

    [Fact]
    public async Task ApplyAsync_FalsePath_TakesPrecedenceOverFalseValue_WhenBothConfigured()
    {
        SetupField("status", "INACTIVE");
        SetupField("dynamic.fallback", "Dynamic Fallback");

        var ctx = MakeContext("""
            {
              "Conditions": [{ "Field": "status", "Operator": "equals", "Value": "ACTIVE" }],
              "FalsePath":  "dynamic.fallback",
              "FalseValue": "Static Fallback"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Dynamic Fallback", result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenTruePathResolvesToNull()
    {
        SetupField("status", "ACTIVE");
        SetupField("missing.field", null);

        var ctx = MakeContext("""
            {
              "Conditions": [{ "Field": "status", "Operator": "equals", "Value": "ACTIVE" }],
              "TruePath": "missing.field"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_Groups_ReturnsTruePathValue_WhenGroupPasses()
    {
        SetupField("status", "ACTIVE");
        SetupField("mode", "TL");
        SetupField("output.approved", "TL Active");

        var ctx = MakeContext("""
            {
              "ConditionGroups": [
                {
                  "Logic": "AND",
                  "Conditions": [
                    { "Field": "status", "Operator": "equals", "Value": "ACTIVE" },
                    { "Field": "mode",   "Operator": "equals", "Value": "TL"     }
                  ]
                }
              ],
              "GroupLogic": "AND",
              "TruePath":  "output.approved",
              "FalseValue": "Rejected"
            }
            """);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("TL Active", result);
    }
}
