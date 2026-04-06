using Moq;
using Transflo.Platform.Transformer.TransformationService.Models;
using Transflo.Platform.Transformer.TransformationService.Services.Interfaces;
using Transflo.Platform.Transformer.TransformationService.Services.Strategies;

namespace Transflo.Platform.Transformer.TransformationService.Tests.Services.Strategies;

public class TemplateTransformationStrategyTests
{
    private readonly Mock<IJsonParserService> _jsonParserMock = new();
    private readonly TemplateTransformationStrategy _sut;

    public TemplateTransformationStrategyTests()
    {
        _sut = new TemplateTransformationStrategy(_jsonParserMock.Object);
    }

    private TransformationContext MakeContext(string? config) =>
        new()
        {
            SourceData = new Dictionary<string, object>(),
            Mapping = new FieldMapping
            {
                SourcePath = "src",
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
    public void TransformationType_Returns_Template()
    {
        Assert.Equal(TransformationType.Template, _sut.TransformationType);
    }

    // ── Basic interpolation ───────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_InterpolatesSinglePlaceholder()
    {
        SetupField("order.id", "ORD-001");
        var ctx = MakeContext("""{"Template":"Order {{order.id}}"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Order ORD-001", result);
    }

    [Fact]
    public async Task ApplyAsync_InterpolatesMultiplePlaceholders()
    {
        SetupField("driver.first_name", "John");
        SetupField("driver.last_name", "Doe");
        var ctx = MakeContext("""{"Template":"Driver: {{driver.first_name}} {{driver.last_name}}"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Driver: John Doe", result);
    }

    [Fact]
    public async Task ApplyAsync_InterpolatesNestedPath()
    {
        SetupField("movement[0].pro_number", "PRO-456");
        var ctx = MakeContext("""{"Template":"PRO: {{movement[0].pro_number}}"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("PRO: PRO-456", result);
    }

    // ── Real-world use cases ──────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_BuildsShipmentLabel()
    {
        SetupField("carrier.name", "FedEx Freight");
        SetupField("movement.pro_number", "PRO-789");
        SetupField("shipment.mode", "LTL");
        var ctx = MakeContext("""{"Template":"{{carrier.name}} | {{shipment.mode}} | PRO: {{movement.pro_number}}"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("FedEx Freight | LTL | PRO: PRO-789", result);
    }

    [Fact]
    public async Task ApplyAsync_BuildsAddressLine()
    {
        SetupField("customer.city", "Atlanta");
        SetupField("customer.state", "GA");
        SetupField("customer.zip", "30301");
        var ctx = MakeContext("""{"Template":"{{customer.city}}, {{customer.state}} {{customer.zip}}"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Atlanta, GA 30301", result);
    }

    [Fact]
    public async Task ApplyAsync_BuildsTrackingReference()
    {
        SetupField("carrier.scac", "FXFE");
        SetupField("order.id", "98765");
        var ctx = MakeContext("""{"Template":"{{carrier.scac}}-{{order.id}}"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("FXFE-98765", result);
    }

    // ── Unresolved placeholders ───────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReplacesUnresolvedPlaceholderWithEmptyString()
    {
        SetupField("order.id", null);
        var ctx = MakeContext("""{"Template":"Order {{order.id}} dispatched"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("Order  dispatched", result);
    }

    [Fact]
    public async Task ApplyAsync_LeavesStaticTextIntact_WhenNoPlaceholders()
    {
        var ctx = MakeContext("""{"Template":"STATIC_VALUE"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("STATIC_VALUE", result);
    }

    // ── Null / invalid config ─────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenConfigIsNull()
    {
        var ctx = MakeContext(null);

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenTemplateKeyMissing()
    {
        var ctx = MakeContext("""{"OtherKey":"value"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyAsync_ReturnsNull_WhenTemplateValueIsEmpty()
    {
        var ctx = MakeContext("""{"Template":""}""");

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

    // ── Repeated placeholder ──────────────────────────────────────────────────

    [Fact]
    public async Task ApplyAsync_ReplacesRepeatedPlaceholder_AllOccurrences()
    {
        SetupField("carrier.scac", "FXFE");
        var ctx = MakeContext("""{"Template":"{{carrier.scac}}/{{carrier.scac}}"}""");

        var result = await _sut.ApplyAsync(ctx);

        Assert.Equal("FXFE/FXFE", result);
    }
}
