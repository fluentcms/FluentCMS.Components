using System.Web;
using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FluentCMS.Components.Browser.Forms;

public sealed class TextInputTests(GalleryFixture gallery)
{
    [Theory]
    [ModeData]
    public async Task TabReachesEachInputThenItsNamedActions(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "text-input");
        IPage page = sample.Page;
        KeyboardDriver keys = page.Keys();
        await keys.StartAtTopAsync();
        await keys.TabToAndAssertPaintChangeAsync("#input-clear", xFromLeft: 0, indicator: page.Locator("#parts .fl-input:has(> #input-clear)"));
        await keys.AssertFocusNameAsync("With a clear action");
        await keys.TabAsync();
        await keys.AssertFocusAsync("#parts .fl-input:has(> #input-clear) > button.fl-input__action");
        await keys.AssertFocusNameAsync("Clear");
        await keys.TabAsync();
        await keys.AssertFocusAsync("#input-password");
        await keys.TabAsync();
        await keys.AssertFocusAsync("#parts .fl-input:has(> #input-password) > button.fl-input__action");
        await keys.AssertFocusNameAsync("Show password");
    }

    [Theory]
    [ModeData]
    public async Task ClearEmptiesTheInputReturnsFocusAndReachesDotNet(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "text-input");
        IPage page = sample.Page;
        KeyboardDriver keys = page.Keys();
        ILocator input = page.Locator("#input-clear");
        ILocator clear = page.Locator("#parts .fl-input:has(> #input-clear) > button.fl-input__action");
        await input.FocusAsync();
        await keys.TabAsync();
        await keys.PressAsync("Enter");
        await Expect(input).ToHaveValueAsync("");
        await keys.AssertFocusAsync("#input-clear");
        await Expect(clear).ToBeVisibleAsync();

        ILocator search = page.Locator("#input-search");
        ILocator count = page.Locator("#input-search-count");
        ILocator searchClear = page.Locator("#search button.fl-search-field__clear");
        await Expect(searchClear).ToBeHiddenAsync();
        await search.FillAsync("report");
        await Expect(searchClear).ToBeVisibleAsync();
        await keys.PressAsync("Tab");
        await keys.AssertFocusAsync("#search button.fl-search-field__clear");
        await Expect(count).ToHaveTextAsync(mode.IsInteractive() ? "2 of 5 projects" : "5 of 5 projects");
        await keys.PressAsync("Space");
        await Expect(search).ToHaveValueAsync("");
        await keys.AssertFocusAsync("#input-search");
        await Expect(searchClear).ToBeHiddenAsync();
        await Expect(count).ToHaveTextAsync("5 of 5 projects");
    }

    [Theory]
    [ModeData]
    public async Task ShowPasswordTogglesTheTypeAndItsPressedState(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "text-input");
        IPage page = sample.Page;
        ILocator input = page.Locator("#input-password");
        ILocator show = page.Locator("#parts .fl-input:has(> #input-password) > button.fl-input__action");
        await Expect(show).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(input).ToHaveAttributeAsync("type", "password");
        await show.FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(show).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(input).ToHaveAttributeAsync("type", "text");
        await Expect(show).ToHaveAccessibleNameAsync("Show password");
        await page.Keyboard.PressAsync("Enter");
        await Expect(show).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(input).ToHaveAttributeAsync("type", "password");
        await Expect(input).ToHaveValueAsync("correct horse battery");
    }

    [Theory]
    [ModeData(RenderMode.Static)]
    public async Task WithoutScriptTheActionsAreLeftOutAndSearchStillSubmits(RenderMode mode)
    {
        await using IBrowserContext context = await gallery.Browser.NewContextAsync(new() { JavaScriptEnabled = false });
        IPage page = await context.NewPageAsync();
        Uri address = new(gallery.BaseAddress, $"{mode.Segment()}/text-input?theme=dark&dir=rtl");
        Assert.True((await page.GotoAsync(address.AbsoluteUri))?.Ok);

        await Expect(page.Locator("main button.fl-input__action")).ToHaveCountAsync(8);
        foreach (ILocator action in await page.Locator("main button.fl-input__action").AllAsync())
        {
            await Expect(action).ToBeHiddenAsync();
        }
        await page.Locator("#input-clear").FocusAsync();
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.Locator("#input-password")).ToBeFocusedAsync();

        await page.Locator("#input-search").FillAsync("brand");
        IResponse response = await page.RunAndWaitForResponseAsync(
            () => page.Locator("#input-search").PressAsync("Enter"),
            res => res.Request.IsNavigationRequest && res.Request.Frame == page.MainFrame);
        Assert.True(response.Ok);
        Assert.Equal("GET", response.Request.Method);
        var query = HttpUtility.ParseQueryString(new Uri(response.Url).Query);
        Assert.Equal("brand", query["q"]);
        Assert.Equal("dark", query["theme"]);
        Assert.Equal("rtl", query["dir"]);
        await Expect(page.Locator("#input-search")).ToHaveValueAsync("brand");
        await Expect(page.Locator("#input-search-count")).ToHaveTextAsync("1 of 5 projects");
        await Expect(page.Locator("#input-search-results li")).ToHaveTextAsync(["Brand refresh"]);
    }

    [Theory]
    [ModeData]
    public async Task ReadOnlyAndDisabledDifferInTheMatrix(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "text-input");
        IPage page = sample.Page;
        ILocator rows = page.Locator("#input-matrix tbody > tr");
        await Expect(rows).ToHaveCountAsync(4);
        ILocator readOnly = rows.Nth(1).Locator("td").Nth(3).Locator("input");
        ILocator disabled = rows.Nth(1).Locator("td").Nth(5).Locator("input");
        await Expect(readOnly).ToBeEditableAsync(new() { Editable = false });
        await Expect(readOnly).ToBeEnabledAsync();
        await Expect(disabled).ToBeDisabledAsync();
        await Expect(rows.Nth(3).Locator("td").Nth(3).Locator("button")).ToHaveCountAsync(0);
        await Expect(rows.Nth(3).Locator("td").Nth(5).Locator("button")).ToBeDisabledAsync();
        await readOnly.FocusAsync();
        await Expect(readOnly).ToBeFocusedAsync();
    }
}
