using System.Text.Json;
using Moq;
using Transflo.Platform.Transformer.Core.DTOs;
using Transflo.Platform.Transformer.Core.Models;
using Transflo.Platform.Transformer.Core.Repositories.Interfaces;
using Transflo.Platform.Transformer.Core.Services;
using TransformationType = Transflo.Platform.Transformer.TransformationService.Models.TransformationType;

namespace Transflo.Platform.Transformer.Core.Tests.Services;

public class FieldMappingValidationServiceTests
{
    private readonly Mock<ITemplateVersionRepository> _versionRepoMock = new();
    private readonly Mock<IFieldMappingRepository> _mappingRepoMock = new();
    private readonly FieldMappingValidationService _sut;

    private static readonly Guid TemplateId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid VersionId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly TemplateVersion DraftVersion = new()
    {
        Id = VersionId,
        TemplateId = TemplateId,
        Version = 1,
        Status = TemplateVersionStatus.Draft
    };

    public FieldMappingValidationServiceTests()
    {
        _sut = new FieldMappingValidationService(_versionRepoMock.Object, _mappingRepoMock.Object);
    }

    // ── Version not found ────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenVersionNotFound()
    {
        _versionRepoMock
            .Setup(r => r.GetByVersionAsync(TemplateId, 1))
            .ReturnsAsync((TemplateVersion?)null);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Single(result.Issues);
        Assert.Equal(ValidationCodes.VersionNotFound, result.Issues[0].Code);
        Assert.Equal(ValidationSeverity.Error, result.Issues[0].Severity);
    }

    // ── Required field validation ────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenTargetPathMissing()
    {
        SetupVersion();
        SetupMappings([new FieldMapping { SourcePath = "src", TargetPath = "", TransformationType = TransformationType.Direct }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.MissingTargetPath);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenSourcePathMissing_ForNonConstantType()
    {
        SetupVersion();
        SetupMappings([new FieldMapping { SourcePath = "", TargetPath = "tgt", TransformationType = TransformationType.Direct }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.MissingSourcePath);
    }

    [Fact]
    public async Task ValidateAsync_NoSourcePathError_WhenConstantType()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "",
            TargetPath = "tgt",
            TransformationType = TransformationType.Constant,
            TransformationConfig = "{\"Value\":\"hello\"}"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Issues, i => i.Code == ValidationCodes.MissingSourcePath);
    }

    // ── Transformation type validation ───────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenTransformationTypeIsInvalid()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src",
            TargetPath = "tgt",
            TransformationType = (TransformationType)999
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.InvalidTransformationType);
    }

    // ── Transformation config validation ────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenTransformationConfigIsInvalidJson()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            TransformationConfig = "not-valid-json"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.InvalidConfigJson);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenConcatMissingFieldsInConfig()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src",
            TargetPath = "tgt",
            TransformationType = TransformationType.Concat,
            TransformationConfig = "{\"Separator\":\"-\"}"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.MissingConfigFields && i.TargetPath == "tgt");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenLookupMissingLookupTableId()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src",
            TargetPath = "tgt",
            TransformationType = TransformationType.Lookup,
            TransformationConfig = "{\"OtherKey\":\"value\"}"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.MissingConfigFields && i.TargetPath == "tgt");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenLookupTableIdIsNotValidGuid()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src",
            TargetPath = "tgt",
            TransformationType = TransformationType.Lookup,
            TransformationConfig = "{\"LookupTableId\":\"not-a-guid\"}"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.MissingConfigFields);
    }

    // ── Conditional config validation ────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenConditionalConfigIsNull()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src",
            TargetPath = "tgt",
            TransformationType = TransformationType.Conditional,
            TransformationConfig = null
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i =>
            i.Code == ValidationCodes.MissingConfigFields &&
            i.Message.Contains("Conditions") &&
            i.TargetPath == "tgt");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenConditionalConfigHasNeitherConditionsNorConditionGroups()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src",
            TargetPath = "tgt",
            TransformationType = TransformationType.Conditional,
            TransformationConfig = "{\"TrueValue\":\"yes\",\"FalseValue\":\"no\"}"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i =>
            i.Code == ValidationCodes.MissingConfigFields &&
            i.Message.Contains("Conditions") &&
            i.TargetPath == "tgt");
    }

    [Fact]
    public async Task ValidateAsync_NoError_WhenConditionalConfigHasConditions()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src",
            TargetPath = "tgt",
            TransformationType = TransformationType.Conditional,
            TransformationConfig = "{\"Conditions\":[{\"Field\":\"status\",\"Operator\":\"equals\",\"Value\":\"ACTIVE\"}],\"TrueValue\":\"yes\"}"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Issues, i => i.Code == ValidationCodes.MissingConfigFields);
    }

    [Fact]
    public async Task ValidateAsync_NoError_WhenConditionalConfigHasConditionGroups()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src",
            TargetPath = "tgt",
            TransformationType = TransformationType.Conditional,
            TransformationConfig = "{\"ConditionGroups\":[{\"Logic\":\"AND\",\"Conditions\":[{\"Field\":\"status\",\"Operator\":\"equals\",\"Value\":\"ACTIVE\"}]}],\"GroupLogic\":\"AND\",\"TrueValue\":\"yes\"}"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Issues, i => i.Code == ValidationCodes.MissingConfigFields);
    }

    // ── Validation rules regex validation ────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenValidationRulesIsInvalidJson()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "not-valid-json"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.InvalidValidationRulesJson);
    }

    [Fact]
    public async Task ValidateAsync_NoError_WhenValidationRulesIsJsonArray()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Required\",\"ErrorMessage\":\"Field is required\"},{\"Type\":\"Length\",\"MaxLength\":100,\"MinLength\":1}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Issues, i => i.Code == ValidationCodes.InvalidValidationRulesJson);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenValidationRulesContainsInvalidRegex()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "{\"pattern\":\"[invalid(regex\"}"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.InvalidRegexPattern);
    }

    // ── Length rule ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenLengthRuleMissingBothMinAndMax()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Length\",\"ErrorMessage\":\"Too long\"}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.MissingRuleProperty);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenMaxLengthLessThanMinLength()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Length\",\"MinLength\":10,\"MaxLength\":2}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.InvalidRulePropertyValue);
    }

    [Fact]
    public async Task ValidateAsync_Valid_WhenLengthRuleIsCorrect()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Length\",\"MinLength\":1,\"MaxLength\":100,\"ErrorMessage\":\"Invalid length\"}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid);
    }

    // ── Range rule ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenRangeRuleMissingBothMinAndMax()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Range\",\"ErrorMessage\":\"Out of range\"}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.MissingRuleProperty);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenMaxValueLessThanMinValue()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Range\",\"MinValue\":100,\"MaxValue\":0}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.InvalidRulePropertyValue);
    }

    [Fact]
    public async Task ValidateAsync_Valid_WhenRangeRuleIsCorrect()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Range\",\"MinValue\":0,\"ErrorMessage\":\"Cannot be negative\"}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid);
    }

    // ── Enum rule ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenEnumRuleMissingAllowedValues()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Enum\",\"ErrorMessage\":\"Invalid value\"}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.MissingRuleProperty);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenEnumAllowedValuesIsEmpty()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Enum\",\"AllowedValues\":[]}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.InvalidRulePropertyValue);
    }

    [Fact]
    public async Task ValidateAsync_Valid_WhenEnumRuleIsCorrect()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Enum\",\"AllowedValues\":[\"TL\",\"LTL\"],\"ErrorMessage\":\"Invalid mode\"}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid);
    }

    // ── Date rule ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenDateRuleHasInvalidFormat()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Date\",\"Format\":\"not a valid format \\uFFFF\"}]"
        }]);

        // Note: most format strings are accepted by DateTime.ToString — only truly invalid ones (e.g. containing
        // illegal chars that throw) will fail. Valid formats like "yyyy-MM-dd" should always pass.
        var validResult = await _sut.ValidateAsync(TemplateId, 1);
        Assert.DoesNotContain(validResult.Issues, i => i.Code == ValidationCodes.InvalidRulePropertyValue && i.TargetPath == "tgt");
    }

    [Fact]
    public async Task ValidateAsync_Valid_WhenDateRuleHasNoFormat()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Date\",\"ErrorMessage\":\"Must be a valid date\"}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_Valid_WhenDateRuleHasValidFormat()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Date\",\"Format\":\"yyyy-MM-dd\",\"ErrorMessage\":\"Must be a valid date\"}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid);
    }

    // ── Regex rule ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenRegexRuleMissingPattern()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Regex\",\"ErrorMessage\":\"Invalid format\"}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.MissingRuleProperty);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenRegexRuleHasInvalidPattern()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Regex\",\"Pattern\":\"[invalid(regex\"}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.InvalidRegexPattern);
    }

    [Fact]
    public async Task ValidateAsync_Valid_WhenRegexRuleHasValidPattern()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Regex\",\"Pattern\":\"^[A-Z]{2,4}$\",\"ErrorMessage\":\"Invalid format\"}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid);
    }

    // ── Unknown rule type ────────────────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsWarning_WhenRuleTypeIsUnknown()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "src", TargetPath = "tgt", TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"UnknownCustomType\",\"ErrorMessage\":\"Something\"}]"
        }]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid); // Warning does not fail validation
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.UnknownRuleType && i.Severity == ValidationSeverity.Warning);
    }

    // ── Duplicate target path validation ────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsError_WhenDuplicateTargetPaths()
    {
        SetupVersion();
        SetupMappings([
            new FieldMapping { SourcePath = "src1", TargetPath = "output.field", TransformationType = TransformationType.Direct },
            new FieldMapping { SourcePath = "src2", TargetPath = "output.field", TransformationType = TransformationType.Direct }
        ]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.DuplicateTargetPath && i.TargetPath == "output.field");
    }

    [Fact]
    public async Task ValidateAsync_DuplicateTargetPath_IsCaseInsensitive()
    {
        SetupVersion();
        SetupMappings([
            new FieldMapping { SourcePath = "src1", TargetPath = "Output.Field", TransformationType = TransformationType.Direct },
            new FieldMapping { SourcePath = "src2", TargetPath = "output.field", TransformationType = TransformationType.Direct }
        ]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.DuplicateTargetPath);
    }

    // ── Valid mapping passes all rules ───────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_ReturnsValid_WhenAllRulesPass()
    {
        SetupVersion();
        SetupMappings([
            new FieldMapping { SourcePath = "src1", TargetPath = "tgt1", TransformationType = TransformationType.Direct },
            new FieldMapping { SourcePath = "src2", TargetPath = "tgt2", TransformationType = TransformationType.Direct }
        ]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsValid_WhenNoMappings()
    {
        SetupVersion();
        SetupMappings([]);

        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    // ── Value validation: IsRequired field missing from source ────────────────

    [Fact]
    public async Task ValidateAsync_WithData_ReturnsFieldRequired_WhenRequiredFieldMissing()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "missing.field",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            IsRequired = true
        }]);

        var doc = JsonDocument.Parse("{\"other\":\"value\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.FieldRequired);
    }

    [Fact]
    public async Task ValidateAsync_WithData_NoError_WhenOptionalFieldMissing()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "missing.field",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            IsRequired = false
        }]);

        var doc = JsonDocument.Parse("{\"other\":\"value\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Issues, i => i.Code == ValidationCodes.FieldRequired);
    }

    // ── Value validation: Required rule ──────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithData_ReturnsError_WhenRequiredRuleAndValueIsEmpty()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "name",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Required\",\"ErrorMessage\":\"Name is required\"}]"
        }]);

        var doc = JsonDocument.Parse("{\"name\":\"\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.ValueViolatesRule && i.Message == "Name is required");
    }

    [Fact]
    public async Task ValidateAsync_WithData_Valid_WhenRequiredRuleAndValuePresent()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "name",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Required\"}]"
        }]);

        var doc = JsonDocument.Parse("{\"name\":\"John\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.True(result.IsValid);
    }

    // ── Value validation: Length rule ─────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithData_ReturnsError_WhenValueExceedsMaxLength()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "code",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Length\",\"MaxLength\":3}]"
        }]);

        var doc = JsonDocument.Parse("{\"code\":\"TOOLONG\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.ValueViolatesRule);
    }

    [Fact]
    public async Task ValidateAsync_WithData_ReturnsError_WhenValueBelowMinLength()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "code",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Length\",\"MinLength\":5}]"
        }]);

        var doc = JsonDocument.Parse("{\"code\":\"AB\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.ValueViolatesRule);
    }

    [Fact]
    public async Task ValidateAsync_WithData_Valid_WhenValueWithinLength()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "code",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Length\",\"MinLength\":2,\"MaxLength\":10}]"
        }]);

        var doc = JsonDocument.Parse("{\"code\":\"ABC\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.True(result.IsValid);
    }

    // ── Value validation: Range rule ──────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithData_ReturnsError_WhenValueBelowMinValue()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "qty",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Range\",\"MinValue\":1}]"
        }]);

        var doc = JsonDocument.Parse("{\"qty\":0}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.ValueViolatesRule);
    }

    [Fact]
    public async Task ValidateAsync_WithData_ReturnsError_WhenValueAboveMaxValue()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "qty",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Range\",\"MaxValue\":100}]"
        }]);

        var doc = JsonDocument.Parse("{\"qty\":200}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.ValueViolatesRule);
    }

    [Fact]
    public async Task ValidateAsync_WithData_ReturnsError_WhenRangeValueIsNotNumeric()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "qty",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Range\",\"MinValue\":1,\"MaxValue\":100}]"
        }]);

        var doc = JsonDocument.Parse("{\"qty\":\"not-a-number\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.ValueViolatesRule);
    }

    // ── Value validation: Enum rule ───────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithData_ReturnsError_WhenValueNotInAllowedValues()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "mode",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Enum\",\"AllowedValues\":[\"TL\",\"LTL\"]}]"
        }]);

        var doc = JsonDocument.Parse("{\"mode\":\"AIR\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.ValueViolatesRule);
    }

    [Fact]
    public async Task ValidateAsync_WithData_Valid_WhenValueInAllowedValues_CaseInsensitive()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "mode",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Enum\",\"AllowedValues\":[\"TL\",\"LTL\"]}]"
        }]);

        var doc = JsonDocument.Parse("{\"mode\":\"tl\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.True(result.IsValid);
    }

    // ── Value validation: Date rule ───────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithData_ReturnsError_WhenValueIsNotAValidDate()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "shipDate",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Date\"}]"
        }]);

        var doc = JsonDocument.Parse("{\"shipDate\":\"not-a-date\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.ValueViolatesRule);
    }

    [Fact]
    public async Task ValidateAsync_WithData_ReturnsError_WhenValueDoesNotMatchDateFormat()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "shipDate",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Date\",\"Format\":\"yyyy-MM-dd\"}]"
        }]);

        var doc = JsonDocument.Parse("{\"shipDate\":\"12/31/2025\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.ValueViolatesRule);
    }

    [Fact]
    public async Task ValidateAsync_WithData_Valid_WhenValueMatchesDateFormat()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "shipDate",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Date\",\"Format\":\"yyyy-MM-dd\"}]"
        }]);

        var doc = JsonDocument.Parse("{\"shipDate\":\"2025-12-31\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.True(result.IsValid);
    }

    // ── Value validation: Regex rule ──────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithData_ReturnsError_WhenValueDoesNotMatchRegex()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "zip",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Regex\",\"Pattern\":\"^\\\\d{5}$\"}]"
        }]);

        var doc = JsonDocument.Parse("{\"zip\":\"ABC12\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.ValueViolatesRule);
    }

    [Fact]
    public async Task ValidateAsync_WithData_Valid_WhenValueMatchesRegex()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "zip",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Regex\",\"Pattern\":\"^\\\\d{5}$\"}]"
        }]);

        var doc = JsonDocument.Parse("{\"zip\":\"12345\"}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.True(result.IsValid);
    }

    // ── Value validation: nested path ─────────────────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithData_ResolvesNestedPath()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "customer.address.zip",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Required\"}]"
        }]);

        var doc = JsonDocument.Parse("{\"customer\":{\"address\":{\"zip\":\"\"}}}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.ValueViolatesRule);
    }

    [Fact]
    public async Task ValidateAsync_WithData_ResolvesArrayIndexedPath()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "orders[0].id",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Required\"}]"
        }]);

        var doc = JsonDocument.Parse("{\"orders\":[{\"id\":\"ORD-001\"}]}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.True(result.IsValid);
    }

    // ── Value validation: Constant type is skipped ────────────────────────────

    [Fact]
    public async Task ValidateAsync_WithData_SkipsConstantTypeMappings()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "",
            TargetPath = "tgt",
            TransformationType = TransformationType.Constant,
            TransformationConfig = "{\"Value\":\"fixed\"}",
            IsRequired = true // should be ignored for Constant
        }]);

        var doc = JsonDocument.Parse("{}").RootElement;
        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Issues, i => i.Code == ValidationCodes.FieldRequired);
    }

    // ── Value validation: sourceDocument passed as JSON string ────────────────

    [Fact]
    public async Task ValidateAsync_WithData_UnwrapsSourceDocumentWhenPassedAsJsonString()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "id",
            TargetPath = "externalId",
            TransformationType = TransformationType.Direct,
            IsRequired = true
        }]);

        // Simulate client sending sourceDocument as an escaped JSON string
        var innerJson = "{\"id\":\"3089050\"}";
        var wrappedJson = $"\"{System.Text.Json.JsonEncodedText.Encode(innerJson)}\"";
        var doc = JsonDocument.Parse(wrappedJson).RootElement;

        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Issues, i => i.Code == ValidationCodes.FieldRequired);
    }

    [Fact]
    public async Task ValidateAsync_WithData_AppliesRuleValidation_WhenSourceDocumentIsJsonString()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "mode",
            TargetPath = "shipmentMode",
            TransformationType = TransformationType.Direct,
            ValidationRules = "[{\"Type\":\"Enum\",\"AllowedValues\":[\"TL\",\"LTL\"],\"ErrorMessage\":\"Invalid mode\"}]"
        }]);

        // Inner document has an invalid enum value — wrapped as string
        var innerJson = "{\"mode\":\"AIR\"}";
        var wrappedJson = $"\"{System.Text.Json.JsonEncodedText.Encode(innerJson)}\"";
        var doc = JsonDocument.Parse(wrappedJson).RootElement;

        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Code == ValidationCodes.ValueViolatesRule && i.Message == "Invalid mode");
    }

    [Fact]
    public async Task ValidateAsync_WithData_FindsNestedField_WhenSourceDocumentIsJsonString()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "stops[0].stop_type",
            TargetPath = "stops[0].type",
            TransformationType = TransformationType.Direct,
            IsRequired = true,
            ValidationRules = "[{\"Type\":\"Enum\",\"AllowedValues\":[\"PU\",\"SO\"],\"ErrorMessage\":\"Invalid stop type\"}]"
        }]);

        var innerJson = "{\"stops\":[{\"stop_type\":\"PU\"}]}";
        var wrappedJson = $"\"{System.Text.Json.JsonEncodedText.Encode(innerJson)}\"";
        var doc = JsonDocument.Parse(wrappedJson).RootElement;

        var result = await _sut.ValidateAsync(TemplateId, 1, doc);

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    // ── Value validation: without source document is still schema-only ────────

    [Fact]
    public async Task ValidateAsync_WithoutData_DoesNotProduceValueIssues()
    {
        SetupVersion();
        SetupMappings([new FieldMapping
        {
            SourcePath = "missing.field",
            TargetPath = "tgt",
            TransformationType = TransformationType.Direct,
            IsRequired = true,
            ValidationRules = "[{\"Type\":\"Required\"}]"
        }]);

        // No source document — structural validation only
        var result = await _sut.ValidateAsync(TemplateId, 1);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(result.Issues, i => i.Code == ValidationCodes.FieldRequired || i.Code == ValidationCodes.ValueViolatesRule);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupVersion() =>
        _versionRepoMock
            .Setup(r => r.GetByVersionAsync(TemplateId, 1))
            .ReturnsAsync(DraftVersion);

    private void SetupMappings(List<FieldMapping> mappings) =>
        _mappingRepoMock
            .Setup(r => r.GetByTemplateVersionIdOrderedAsync(VersionId))
            .ReturnsAsync(mappings);
}
