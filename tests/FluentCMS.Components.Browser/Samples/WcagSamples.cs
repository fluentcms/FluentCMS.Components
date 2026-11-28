using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;

namespace FluentCMS.Components.Browser.Samples;

public sealed class WcagSamples(GalleryFixture gallery)
{
    [Theory]
    [ModeData(Pages = ["", "tokens", "render-modes"])]
    public Task TargetsAreAtLeast24By24InComfortableDensity(RenderMode mode, string page) =>
        CheckTargetSizesAsync(mode, page, "comfortable");

    [Theory]
    [ModeData(Pages = ["", "tokens", "render-modes"])]
    public Task TargetsAreAtLeast24By24InCompactDensity(RenderMode mode, string page) =>
        CheckTargetSizesAsync(mode, page, "compact");

    private async Task CheckTargetSizesAsync(RenderMode mode, string page, string density)
    {
        await using GalleryPage open = await gallery.OpenAsync(mode, page, new GalleryOptions { Density = density });

        Assert.Equal(density, await open.Page.Locator("html").GetAttributeAsync("data-fl-density"));
        string controlHeight = await open.Page.Locator("html").EvaluateAsync<string>(
            "el => getComputedStyle(el).getPropertyValue('--fl-control-height-m').trim()");
        Assert.Equal(density == "compact" ? "1.75rem" : "2rem", controlHeight);
        await Wcag.AssertTargetSizesAsync(open.Page);
    }

    [Theory]
    [ModeData(Pages = ["", "tokens", "render-modes"])]
    public async Task PageDoesNotScrollSidewaysAt320(RenderMode mode, string page)
    {
        await using GalleryPage open = await gallery.OpenAsync(mode, page);

        await Wcag.AssertNoHorizontalScrollAsync(open.Page);
    }

    [Theory]
    [ModeData(Pages = ["", "tokens", "render-modes"])]
    public async Task NothingMovesUnderReducedMotion(RenderMode mode, string page)
    {
        await using GalleryPage open = await gallery.OpenAsync(mode, page, new GalleryOptions { ReducedMotion = true });

        ILocator link = open.Page.Locator(".gallery-nav a:not([aria-current])").First;
        await Wcag.AssertNoMotionAsync(open.Page, () => link.HoverAsync());
        Assert.True(await link.EvaluateAsync<bool>("el => el.matches(':hover')"),
            "The navigation link was not hovered during the reduced-motion check.");
    }
}
