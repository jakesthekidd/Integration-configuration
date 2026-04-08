using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

public class PrefixMapTransformationStrategyTests
{
    private readonly PrefixMapTransformationStrategy _sut = new();

    private static TransformationContext MakeContext(
        string sourcePrefix,
        string? config,
        Dictionary<string, object>? sourceData = null) =>
        new()
        {
            SourceData = sourceData ?? new Dictionary<string, object>(),
            Mapping = new FieldMapping
            {
                SourcePath = sourcePrefix,
                TargetPath = "drivers",
                TransformationConfig = config
            }
        };

    private static Dictionary<string, object> Source(params (string Key, string Value)[] entries) =>
        entries.ToDictionary(e => e.Key, e => (object)e.Value);

    // ── Identity ──────────────────────────────────────────────────────────────

    [Fact]
    public void TransformationType_Returns_PrefixMap()
    {
        Assert.Equal(TransformationType.PrefixMap, _sut.TransformationType);
    }

    // ── Real-world use case ───────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_MapsDeliveryDrivers_ToStructuredArray()
    {
        var source = Source(
            ("deliveryDriver1", "Ateeq test"),
            ("deliveryDriver2", "Ateeq test1"));

        var ctx = MakeContext("deliveryDriver",
            """{"Fields":["firstName","lastName"]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Equal(2, list.Count);

        Assert.Equal("Ateeq", list[0]["firstName"]);
        Assert.Equal("test",  list[0]["lastName"]);
        Assert.Equal("Ateeq", list[1]["firstName"]);
        Assert.Equal("test1", list[1]["lastName"]);
    }

    // ── Multiple entries ──────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsAllMatchingEntries_InSortedOrder()
    {
        var source = Source(
            ("driver3", "Charlie Brown"),
            ("driver1", "Alice Smith"),
            ("driver2", "Bob Jones"));

        var ctx = MakeContext("driver",
            """{"Fields":["firstName","lastName"]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Equal(3, list.Count);
        Assert.Equal("Alice",   list[0]["firstName"]);
        Assert.Equal("Bob",     list[1]["firstName"]);
        Assert.Equal("Charlie", list[2]["firstName"]);
    }

    // ── Single entry ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsSingleEntry_WhenOnlyOneKeyMatches()
    {
        var source = Source(("pickupDriver1", "John Doe"));
        var ctx = MakeContext("pickupDriver",
            """{"Fields":["firstName","lastName"]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Single(list);
        Assert.Equal("John", list[0]["firstName"]);
        Assert.Equal("Doe",  list[0]["lastName"]);
    }

    // ── Custom separator ──────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_SplitsOnCustomSeparator()
    {
        var source = Source(("driver1", "John,Doe,Jr"));
        var ctx = MakeContext("driver",
            """{"Fields":["firstName","lastName","suffix"],"Separator":","}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Equal("John", list[0]["firstName"]);
        Assert.Equal("Doe",  list[0]["lastName"]);
        Assert.Equal("Jr",   list[0]["suffix"]);
    }

    [Fact]
    public async Task ApplyAsync_SplitsOnMultiCharSeparator()
    {
        var source = Source(("driver1", "John::Doe"));
        var ctx = MakeContext("driver",
            """{"Fields":["firstName","lastName"],"Separator":"::"}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Equal("John", list[0]["firstName"]);
        Assert.Equal("Doe",  list[0]["lastName"]);
    }

    // ── Partial / extra parts ─────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_SetsNull_WhenFewerPartsThanFields()
    {
        var source = Source(("driver1", "Mononym"));
        var ctx = MakeContext("driver",
            """{"Fields":["firstName","lastName"]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Equal("Mononym", list[0]["firstName"]);
        Assert.Null(list[0]["lastName"]);
    }

    [Fact]
    public async Task ApplyAsync_IgnoresExtraParts_WhenMorePartsThanFields()
    {
        var source = Source(("driver1", "John Middle Doe Extra"));
        var ctx = MakeContext("driver",
            """{"Fields":["firstName","lastName"]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Equal("John",   list[0]["firstName"]);
        Assert.Equal("Middle", list[0]["lastName"]);
        Assert.Equal(2, list[0].Count);
    }

    // ── SkipEmpty ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_IncludesEmptyValues_WhenSkipEmptyIsFalse()
    {
        var source = Source(
            ("driver1", "John Doe"),
            ("driver2", ""));

        var ctx = MakeContext("driver",
            """{"Fields":["firstName","lastName"],"SkipEmpty":"false"}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task ApplyAsync_IncludesEmptyValues_WhenSkipEmptyIsAbsent()
    {
        var source = Source(
            ("driver1", "John Doe"),
            ("driver2", ""));

        var ctx = MakeContext("driver",
            """{"Fields":["firstName","lastName"]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task ApplyAsync_OmitsEmptyValues_WhenSkipEmptyIsTrue()
    {
        var source = Source(
            ("driver1", "John Doe"),
            ("driver2", ""),
            ("driver3", "   "),
            ("driver4", "Jane Smith"));

        var ctx = MakeContext("driver",
            """{"Fields":["firstName","lastName"],"SkipEmpty":"true"}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Equal(2, list.Count);
        Assert.Equal("John",  list[0]["firstName"]);
        Assert.Equal("Jane",  list[1]["firstName"]);
    }

    // ── Prefix matching precision ─────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ExcludesExactPrefixMatch_RequiresAtLeastOneExtraChar()
    {
        var source = Source(
            ("driver",  "Exact Match"),  // same as prefix — excluded
            ("driver1", "John Doe"));    // prefix + suffix — included

        var ctx = MakeContext("driver",
            """{"Fields":["firstName","lastName"]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Single(list);
        Assert.Equal("John", list[0]["firstName"]);
    }

    [Fact]
    public async Task ApplyAsync_IsCaseInsensitive_ForPrefixMatching()
    {
        var source = Source(
            ("DeliveryDriver1", "John Doe"),
            ("DELIVERYDRIVER2", "Jane Smith"));

        var ctx = MakeContext("deliveryDriver",
            """{"Fields":["firstName","lastName"]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task ApplyAsync_DoesNotMatchUnrelatedKeys()
    {
        var source = Source(
            ("driver1",   "John Doe"),
            ("otherKey1", "should be ignored"),
            ("notdriver1","also ignored"));

        var ctx = MakeContext("driver",
            """{"Fields":["firstName","lastName"]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Single(list);
        Assert.Equal("John", list[0]["firstName"]);
    }

    // ── Edge cases: no matches ────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenNoKeysMatchPrefix()
    {
        var source = Source(("otherField", "value"));
        var ctx = MakeContext("driver",
            """{"Fields":["firstName","lastName"]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenSourceDataIsEmpty()
    {
        var ctx = MakeContext("driver",
            """{"Fields":["firstName","lastName"]}""",
            new Dictionary<string, object>());

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    // ── Edge cases: invalid config ────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConfigIsNull()
    {
        var source = Source(("driver1", "John Doe"));
        var ctx = MakeContext("driver", null, source);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConfigIsInvalidJson()
    {
        var source = Source(("driver1", "John Doe"));
        var ctx = MakeContext("driver", "not-json", source);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenFieldsKeyIsMissing()
    {
        var source = Source(("driver1", "John Doe"));
        var ctx = MakeContext("driver",
            """{"Separator":" "}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenFieldsArrayIsEmpty()
    {
        var source = Source(("driver1", "John Doe"));
        var ctx = MakeContext("driver",
            """{"Fields":[]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenSourcePrefixIsEmpty()
    {
        var source = Source(("driver1", "John Doe"));
        var ctx = MakeContext("",
            """{"Fields":["firstName","lastName"]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    // ── Single field mapping ──────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_MapsSingleField_WhenOnlyOneFieldSpecified()
    {
        var source = Source(
            ("tag1", "urgent"),
            ("tag2", "fragile"));

        var ctx = MakeContext("tag",
            """{"Fields":["value"]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Equal(2, list.Count);
        Assert.Equal("urgent",  list[0]["value"]);
        Assert.Equal("fragile", list[1]["value"]);
    }

    // ── Three-field split ─────────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_MapsThreeFields_FromSpaceSeparatedValue()
    {
        var source = Source(("contact1", "John Michael Doe"));
        var ctx = MakeContext("contact",
            """{"Fields":["firstName","middleName","lastName"]}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        var list = Assert.IsType<List<Dictionary<string, object?>>>(result);
        Assert.Equal("John",    list[0]["firstName"]);
        Assert.Equal("Michael", list[0]["middleName"]);
        Assert.Equal("Doe",     list[0]["lastName"]);
    }

    // ── All-empty after SkipEmpty ─────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenAllEntriesSkippedBySkipEmpty()
    {
        var source = Source(
            ("driver1", ""),
            ("driver2", "   "));

        var ctx = MakeContext("driver",
            """{"Fields":["firstName","lastName"],"SkipEmpty":"true"}""",
            source);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }
}
