using FluentCMS.Components.Browser.Harness;

namespace FluentCMS.Components.Browser.Samples;

public sealed class ModeMatrixSamples(GalleryFixture gallery)
{
    [Theory]
    [ModeData]
    public async Task EveryModeBecomesWhatItsAddressNames(RenderMode mode)
    {
        await using GalleryPage page = await gallery.OpenAsync(mode, "render-modes");

        string? rendered = await page.Page.Locator("main").GetAttributeAsync("data-render-mode");
        string? interactive = await page.Page.Locator("main").GetAttributeAsync("data-interactive");

        Assert.Equal(mode.Segment(), rendered);
        Assert.Equal(mode.IsInteractive() ? "true" : "false", interactive);
    }
}
