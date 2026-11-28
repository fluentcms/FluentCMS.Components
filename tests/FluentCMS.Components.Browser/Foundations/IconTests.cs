using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;

namespace FluentCMS.Components.Browser.Foundations;

public sealed class IconTests(GalleryFixture gallery)
{
    [Theory]
    [ModeData]
    public Task IconPagePassesAxe(RenderMode mode) =>
        Accessibility.AssertNoViolationsAsync(gallery, mode, "icon");

    [Theory]
    [ModeData]
    public async Task IconPageReflowsWithUsableTargets(RenderMode mode)
    {
        foreach (string density in new[] { "comfortable", "compact" })
        {
            await using GalleryPage open = await gallery.OpenAsync(mode, "icon", new GalleryOptions { Density = density });
            Assert.NotEmpty(await open.Page.Locator("main svg.fl-icon path").AllAsync());
            await Wcag.AssertTargetSizesAsync(open.Page);
            await Wcag.AssertNoHorizontalScrollAsync(open.Page);
        }
    }

    [Theory]
    [ModeData]
    public async Task SpinnerStopsUnderReducedMotion(RenderMode mode)
    {
        await using GalleryPage open = await gallery.OpenAsync(mode, "icon");
        ILocator spinner = open.Page.Locator("main svg.fl-icon--spin").First;
        Assert.Equal("fl-icon-spin", await spinner.EvaluateAsync<string>("el => getComputedStyle(el).animationName"));

        await Wcag.AssertNoMotionAsync(open.Page);
        Assert.Equal("none", await spinner.EvaluateAsync<string>("el => getComputedStyle(el).animationName"));
    }
}
