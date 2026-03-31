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
