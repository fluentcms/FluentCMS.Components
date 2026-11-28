using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;

namespace FluentCMS.Components.Browser.Samples;

public sealed class KeyboardSamples(GalleryFixture gallery)
{
    [Theory]
    [ModeData]
    public async Task HeaderNavigationWorksWithTheKeyboardAlone(RenderMode mode)
    {
        await using GalleryPage page = await gallery.OpenAsync(mode, "");
        KeyboardDriver keys = page.Page.Keys();
        await keys.StartAtTopAsync();

        await keys.TabToAndAssertPaintChangeAsync("nav[aria-label='Pages'] a[href^='tokens']", xFromLeft: -3);
        await keys.AssertFocusNameAsync("Tokens");

        await keys.PressAsync("Enter");

        // The address changes before the page does when the host enhances the navigation.
        await page.Page.Locator("main h1", new PageLocatorOptions { HasTextString = "Tokens" }).WaitForAsync();
        Assert.Contains($"/{mode.Segment()}/tokens", page.Page.Url);
    }

    [Theory]
    [ModeData]
    public async Task ShiftTabGoesBackTheWayTabCame(RenderMode mode)
    {
        await using GalleryPage page = await gallery.OpenAsync(mode, "");
        KeyboardDriver keys = page.Page.Keys();
        await keys.StartAtTopAsync();

        await keys.TabAsync();
        FocusInfo first = await keys.FocusAsync();
        await keys.TabAsync();
        await keys.ShiftTabAsync();

        Assert.Equal(first, await keys.FocusAsync());
    }
}
