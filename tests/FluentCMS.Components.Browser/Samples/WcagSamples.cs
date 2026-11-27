using FluentCMS.Components.Browser.Harness;

namespace FluentCMS.Components.Browser.Samples;

public sealed class WcagSamples(GalleryFixture gallery)
{
    [Theory]
    [ModeData(Pages = ["", "tokens", "render-modes"])]
    public async Task TargetsAreAtLeast24By24(RenderMode mode, string page)
    {
        await using GalleryPage open = await gallery.OpenAsync(mode, page);

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

        await Wcag.AssertNoMotionAsync(open.Page);
    }
}
