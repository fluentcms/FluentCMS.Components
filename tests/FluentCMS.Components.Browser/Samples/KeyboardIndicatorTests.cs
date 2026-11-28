using FluentCMS.Components.Browser.Harness;
using Xunit.Sdk;

namespace FluentCMS.Components.Browser.Samples;

public sealed class KeyboardIndicatorTests(GalleryFixture gallery)
{
    [Fact]
    public async Task LabelledByWinsOverAriaLabelOnTheKeyboardStop()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync("""
            <span id="label">Delete</span>
            <button aria-label="Save" aria-labelledby="label">Action</button>
            """);
        KeyboardDriver keys = page.Page.Keys();
        await keys.TabAsync();

        FocusInfo focus = await keys.FocusAsync();
        await keys.AssertFocusNameAsync("Delete");
        Assert.Contains("\"Delete\"", focus.ToString());
    }

    [Theory]
    [InlineData("button:focus-visible { outline: 2px solid transparent }")]
    [InlineData("button { box-shadow: 0 0 0 2px black }")]
    public async Task KeyboardDoesNotShowAnIndicatorWhenThePaintIsUnchanged(string css)
    {
        await using GalleryPage page = await gallery.OpenBlankAsync($"<style>button {{ outline: none }} {css}</style><button>Save</button>");
        KeyboardDriver keys = page.Page.Keys();
        XunitException failure = await Assert.ThrowsAnyAsync<XunitException>(() => keys.TabToAndAssertPaintChangeAsync("button", xFromLeft: -1));
        Assert.Contains("indicator pixel", failure.Message);
        Assert.Contains("stayed", failure.Message);
    }

    [Fact]
    public async Task KeyboardShowsABackgroundOnlyIndicator()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync("""
            <style>
              button { background: white; outline: none; padding: 8px }
              button:focus-visible { background: gold }
            </style>
            <button>Save</button>
            """);
        KeyboardDriver keys = page.Page.Keys();
        await keys.TabToAndAssertPaintChangeAsync("button", xFromLeft: 5);
    }

    [Theory]
    [ModeData]
    public async Task KeyboardShowsTheLibraryInputRingOnItsWrapper(RenderMode mode)
    {
        await using GalleryPage page = await gallery.OpenAsync(mode, "tokens");
        KeyboardDriver keys = page.Page.Keys();
        await keys.StartAtTopAsync();
        const string input = ".fl-input__control[aria-label='Sample']";
        await keys.TabToAndAssertPaintChangeAsync(input, xFromLeft: 0, indicator: page.Page.Locator($".fl-input:has(> {input})"));
    }
}
