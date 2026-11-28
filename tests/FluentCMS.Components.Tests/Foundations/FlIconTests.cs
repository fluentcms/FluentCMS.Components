using Bunit;

namespace FluentCMS.Components.Tests.Foundations;

public sealed class FlIconTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void RendersTrustedHostDrawingInlineWithDecorativeDefaults()
    {
        const string drawing = """<g><circle cx="12" cy="12" r="3" /></g>""";
        var cut = _context.Render<FlIcon>(parameters => parameters.Add(p => p.Icon, new FlIconDefinition(drawing)));

        cut.MarkupMatches($"""<svg class="fl-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">{drawing}</svg>""");
        Assert.Equal(FlSize.Medium, cut.Instance.Size);
    }

    [Theory]
    [InlineData(FlSize.Small, "fl-icon fl-icon--s")]
    [InlineData(FlSize.Medium, "fl-icon")]
    [InlineData(FlSize.Large, "fl-icon fl-icon--l")]
    public void AppliesOnlyTheSelectedSize(FlSize size, string classes)
    {
        var cut = _context.Render<FlIcon>(parameters => parameters
            .Add(p => p.Icon, FlIcons.ArrowRight).Add(p => p.Size, size));

        Assert.Equal(classes, cut.Find("svg").ClassName);
    }

    [Fact]
    public void MergesHostAttributesWithDirectionAndSpinWithoutMutatingTheHost()
    {
        var attributes = new Dictionary<string, object> { ["CLASS"] = "host first", ["id"] = "next", ["data-extra"] = "42" };
        var cut = _context.Render<FlIcon>(parameters => parameters
            .Add(p => p.Icon, FlIcons.ArrowRight).Add(p => p.Directional, true).Add(p => p.Spin, true)
            .Add(p => p.AdditionalAttributes, attributes));

        var svg = cut.Find("svg#next[data-extra='42']");
        Assert.Equal("fl-icon fl-icon--directional fl-icon--spin host first", svg.ClassName);
        Assert.Equal("host first", attributes["CLASS"]);
    }

    [Theory]
    [InlineData("Search & find")]
    [InlineData(null)]
    [InlineData("  ")]
    public void HostCannotOverrideNamingGeometryOrFocus(string? label)
    {
        var cut = _context.Render<FlIcon>(parameters => parameters
            .Add(p => p.Icon, FlIcons.Search).Add(p => p.Label, label)
            .AddUnmatched("viewBOX", "0 0 1 1").AddUnmatched("role", "button")
            .AddUnmatched("aria-LABEL", "other").AddUnmatched("ARIA-labelledby", "other")
            .AddUnmatched("aria-HIDDEN", "false").AddUnmatched("FOCUSABLE", "true")
            .AddUnmatched("tabIndex", "0").AddUnmatched("autofocus", true).AddUnmatched("contenteditable", "true"));

        var svg = cut.Find("svg");
        bool named = !string.IsNullOrWhiteSpace(label);
        Assert.Equal("0 0 24 24", svg.GetAttribute("viewBox"));
        Assert.Equal(named ? "img" : null, svg.GetAttribute("role"));
        Assert.Equal(named ? label : null, svg.GetAttribute("aria-label"));
        Assert.Equal(named ? null : "true", svg.GetAttribute("aria-hidden"));
        Assert.Equal("false", svg.GetAttribute("focusable"));
        Assert.False(svg.HasAttribute("tabindex"));
        Assert.False(svg.HasAttribute("autofocus"));
        Assert.False(svg.HasAttribute("contenteditable"));
        Assert.False(svg.HasAttribute("aria-labelledby"));
    }

    [Fact]
    public void DoesNotMakeSvgFocusableWithInlineFocusHandlers()
    {
        var cut = _context.Render<FlIcon>(parameters => parameters.Add(p => p.Icon, FlIcons.Search)
            .AddUnmatched("onFocus", "void 0").AddUnmatched("onblur", "void 0")
            .AddUnmatched("onfocusin", "void 0").AddUnmatched("onfocusout", "void 0")
            .AddUnmatched("onmouseenter", "void 0"));

        var svg = cut.Find("svg");
        Assert.False(svg.HasAttribute("onfocus"));
        Assert.False(svg.HasAttribute("onblur"));
        Assert.False(svg.HasAttribute("onfocusin"));
        Assert.False(svg.HasAttribute("onfocusout"));
        Assert.Equal("void 0", svg.GetAttribute("onmouseenter"));
    }

    [Fact]
    public void RerenderReplacesDrawingNameAndModifiers()
    {
        var cut = _context.Render<FlIcon>(parameters => parameters.Add(p => p.Icon, FlIcons.Search)
            .Add(p => p.Directional, true).Add(p => p.Label, "Find"));

        cut.Render(parameters => parameters.Add(p => p.Icon, FlIcons.Check)
            .Add(p => p.Directional, false).Add(p => p.Label, null));

        cut.MarkupMatches($"""<svg class="fl-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">{FlIcons.Check.Content}</svg>""");
    }

    public void Dispose() => _context.Dispose();
}
