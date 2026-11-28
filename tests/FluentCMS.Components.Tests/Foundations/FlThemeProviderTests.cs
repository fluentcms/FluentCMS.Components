using Bunit;

namespace FluentCMS.Components.Tests.Foundations;

public sealed class FlThemeProviderTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void RendersAPlainWrapperWhenAllSettingsInherit()
    {
        var cut = _context.Render<FlThemeProvider>(p => p.AddChildContent<FlButton>(b => b.AddChildContent("Save")));

        cut.MarkupMatches("""<div><button type="button" class="fl-button">Save</button></div>""");
    }

    [Theory]
    [InlineData(FlTheme.Light, FlDensity.Comfortable, FlDirection.Ltr, "light", "comfortable", "ltr")]
    [InlineData(FlTheme.Dark, FlDensity.Compact, FlDirection.Rtl, "dark", "compact", "rtl")]
    public void MapsExplicitSettingsEvenWhenTheyRestoreDefaults(
        FlTheme theme, FlDensity density, FlDirection direction, string themeValue, string densityValue, string directionValue)
    {
        var cut = _context.Render<FlThemeProvider>(p => p.Add(x => x.Theme, theme)
            .Add(x => x.Density, density).Add(x => x.Direction, direction));

        cut.MarkupMatches($"""<div data-fl-theme="{themeValue}" data-fl-density="{densityValue}" dir="{directionValue}"></div>""");
    }

    [Fact]
    public void ForwardsHostAttributesWithoutLettingThemReplaceOwnedSettings()
    {
        var attributes = new Dictionary<string, object>
        {
            ["id"] = "preview",
            ["class"] = "brand",
            ["data-fl-accent"] = "brand",
            ["DATA-FL-THEME"] = "dark",
            ["Data-Fl-Density"] = "compact",
            ["DIR"] = "rtl",
        };
        var cut = _context.Render<FlThemeProvider>(p => p.Add(x => x.AdditionalAttributes, attributes)
            .Add(x => x.Theme, FlTheme.Light));

        cut.MarkupMatches("""<div id="preview" class="brand" data-fl-accent="brand" data-fl-theme="light"></div>""");
        Assert.Equal(6, attributes.Count);
    }

    [Fact]
    public void NullRestoresInheritanceWithoutReplacingTheWrapperOrChild()
    {
        var cut = _context.Render<FlThemeProvider>(p => p.Add(x => x.Theme, FlTheme.Dark)
            .Add(x => x.Density, FlDensity.Compact).Add(x => x.Direction, FlDirection.Rtl)
            .AddChildContent<FlButton>(b => b.AddChildContent("Save")));
        var child = cut.FindComponent<FlButton>().Instance;
        Assert.Equal("dark", cut.Find("div").GetAttribute("data-fl-theme"));

        cut.Render(p => p.Add(x => x.Theme, null).Add(x => x.Density, null).Add(x => x.Direction, null));

        cut.MarkupMatches("""<div><button type="button" class="fl-button">Save</button></div>""");
        Assert.Same(child, cut.FindComponent<FlButton>().Instance);
    }

    public void Dispose() => _context.Dispose();
}
