using System.Collections.Concurrent;
using System.Web;
using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FluentCMS.Components.Browser.Foundations;

public sealed class ButtonTests(GalleryFixture gallery)
{
    [Theory]
    [ModeData]
    public async Task ActionAndControlledToggleRespondToKeyboard(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "button");
        IPage page = sample.Page;
        KeyboardDriver keys = page.Keys();
        ILocator count = page.Locator("#button-count");
        await Expect(page.Locator("main [data-level=static]")).ToBeVisibleAsync();
        await Expect(count).ToHaveTextAsync("0");
        await keys.StartAtTopAsync();
        ILocator toggle = page.Locator("#button-toggle");
        await keys.TabToAsync("#button-toggle", maxPresses: 120);
        await keys.AssertFocusNameAsync("Italic");
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", "false");
        await keys.PressAsync("Space");
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", mode.IsInteractive() ? "true" : "false");
        await keys.PressAsync("Enter");
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", "false");

        // Unlike Italic, this specimen's owner supplies a fixed Pressed value and no callback.
        const string fixedToggle = "#button-matrix tbody tr:first-child td:nth-of-type(5) button";
        await keys.TabToAsync(fixedToggle);
        await Expect(page.Locator(fixedToggle)).ToHaveAttributeAsync("aria-pressed", "true");
        await keys.PressAsync("Space");

        await keys.TabToAndAssertPaintChangeAsync("#button-action", xFromLeft: -3);
        await keys.AssertFocusNameAsync("Count a click");
        await keys.PressAsync("Enter");
        await Expect(count).ToHaveTextAsync(mode.IsInteractive() ? "1" : "0");
        await keys.PressAsync("Space");
        await Expect(count).ToHaveTextAsync(mode.IsInteractive() ? "2" : "0");
        await Expect(page.Locator(fixedToggle)).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [Theory]
    [ModeData]
    public async Task UnavailableControlsTakeNoAction(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "button");
        IPage page = sample.Page;
        string originalUrl = page.Url;
        ILocator field = page.Locator("#button-reset-field");
        await field.FillAsync("unsaved edit");
        await using IJSHandle form = await field.EvaluateHandleAsync("el => el.form");
        foreach (string selector in new[] { "#button-busy", "#button-disabled", "#button-busy-reset" })
        {
            Assert.True(await page.Locator(selector).EvaluateAsync<bool>("(el, form) => form !== null && el.form === form", form));
        }

        ILocator busy = page.Locator("#button-busy");
        await Expect(busy).ToHaveAccessibleNameAsync("Save changes");
        await Expect(busy).ToHaveAttributeAsync("aria-busy", "true");
        ILocator disabled = page.Locator("#button-disabled");
        await Expect(disabled).ToHaveAccessibleNameAsync("Save changes");
        Assert.True(await disabled.EvaluateAsync<bool>("el => el.matches('button:disabled')"));

        foreach (string selector in new[] { "#button-busy-link", "#button-disabled-link" })
        {
            ILocator link = page.Locator(selector);
            await Expect(link).ToHaveRoleAsync(AriaRole.Link);
            await Expect(link).ToHaveAccessibleNameAsync("Open");
            await Expect(link).ToHaveAttributeAsync("aria-disabled", "true");
            Assert.Null(await link.GetAttributeAsync("href"));
        }

        KeyboardDriver keys = page.Keys();
        await page.Locator("#button-action").FocusAsync();
        await keys.TabToAsync("#button-busy-link");
        await keys.PressAsync("Enter");
        await keys.TabAsync();
        await keys.AssertFocusAsync("#button-reset-field"); // Skips the disabled link.
        await keys.TabToAsync("#button-busy");
        await keys.PressAsync("Enter");
        await keys.PressAsync("Space");
        await keys.TabAsync();
        await keys.AssertFocusAsync("#button-busy-reset"); // Skips the disabled submit.
        await keys.PressAsync("Enter");
        await keys.PressAsync("Space");

        // Bypass Playwright's own disabled check, not the browser's native hit testing.
        await disabled.ClickAsync(new() { Force = true });
        await page.Locator("#button-disabled-link").ClickAsync(new() { Force = true });
        Assert.Equal(originalUrl, page.Url);
        Assert.True(await form.EvaluateAsync<bool>("el => el.isConnected"));
        await Expect(field).ToHaveValueAsync("unsaved edit");
        // A separate callback acts as a round-trip barrier without changing the counter under test.
        ILocator toggle = page.Locator("#button-toggle");
        await toggle.ClickAsync();
        await Expect(toggle).ToHaveAttributeAsync("aria-pressed", mode.IsInteractive() ? "true" : "false");
        await Expect(page.Locator("#button-count")).ToHaveTextAsync("0");
    }

    [Theory]
    [ModeData]
    public async Task EnterFollowsNativeLinkInTheCurrentMode(RenderMode mode)
    {
        var options = new GalleryOptions { Theme = "dark", Density = "compact", Direction = "rtl" };
        await using GalleryPage sample = await gallery.OpenAsync(mode, "button", options);
        IPage page = sample.Page;
        ILocator link = page.Locator("a#button-link[href]");
        await Expect(link).ToHaveRoleAsync(AriaRole.Link);
        await Expect(link).ToHaveAccessibleNameAsync("Open");
        Uri destination = new(gallery.BaseAddress, $"{mode.Segment()}/icon{options.Query}");
        await Expect(link).ToHaveJSPropertyAsync("href", destination.AbsoluteUri);
        KeyboardDriver keys = page.Keys();
        await keys.TabToAsync("#button-link", maxPresses: 120);
        await keys.PressAsync("Enter");
        await Expect(page).ToHaveURLAsync(destination.AbsoluteUri);
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Icon", Exact = true, Level = 1 })).ToBeVisibleAsync();
    }

    [Theory]
    [ModeData(RenderMode.Static)]
    public async Task NoScriptFormNavigatesOnlyForItsAvailableSubmitter(RenderMode mode)
    {
        await using IBrowserContext context = await gallery.Browser.NewContextAsync(new() { JavaScriptEnabled = false });
        IPage page = await context.NewPageAsync();
        Uri address = new(gallery.BaseAddress, $"{mode.Segment()}/button?theme=dark&density=compact&dir=rtl");
        Assert.True((await page.GotoAsync(address.AbsoluteUri))?.Ok);
        var documents = new ConcurrentQueue<string>();
        page.Request += (_, request) =>
        {
            if (request.IsNavigationRequest && request.Frame == page.MainFrame)
            {
                documents.Enqueue(request.Url);
            }
        };

        await page.Locator("#button-busy").PressAsync("Enter");
        await page.Locator("#button-busy").PressAsync("Space");
        Assert.Empty(documents);
        Assert.Equal(address.AbsoluteUri, page.Url);
        IResponse response = await page.RunAndWaitForResponseAsync(
            () => page.Locator("#button-submit").PressAsync("Enter"),
            res => res.Request.IsNavigationRequest && res.Request.Frame == page.MainFrame);
        Assert.True(response.Ok);
        Assert.Equal("document", response.Request.ResourceType);
        Assert.Equal("GET", response.Request.Method);
        Assert.Equal(response.Url, Assert.Single(documents));
        Uri result = new(response.Url);
        Assert.Equal(address.AbsolutePath, result.AbsolutePath);
        var query = HttpUtility.ParseQueryString(result.Query);
        Assert.Equal("submitted", query["button-result"]);
        Assert.Equal("dark", query["theme"]);
        Assert.Equal("compact", query["density"]);
        Assert.Equal("rtl", query["dir"]);
        await Expect(page.GetByText("Example submitted.", new() { Exact = true })).ToBeVisibleAsync();
    }
}
