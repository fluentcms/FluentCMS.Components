using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FluentCMS.Components.Browser;

public sealed class ScriptTests(GalleryFixture gallery)
{
    private const string Number = "#script-number";
    private const string Checkbox = "#script-checkbox";
    private const string Step = ".fl-number-input__step";

    [Theory]
    [ModeData]
    public async Task PopoverFollowsNativeOpenAndDismiss(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "script");
        await CheckPopoverAsync(sample.Page);
    }

    [Theory]
    [ModeData]
    public async Task MixedCheckboxRemainsANativeCheckbox(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "script");
        await CheckCheckboxAsync(sample.Page);
    }

    [Theory]
    [ModeData]
    public async Task StepButtonsUpdateLimitsAndReportChanges(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "script");
        await CheckNumberAsync(sample.Page, mode.IsInteractive());
    }

    [Theory]
    [ModeData(RenderMode.Static)]
    public async Task EnhancedNavigationKeepsTheDocumentAndEnhancesNewControls(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "tokens");
        IPage page = sample.Page;
        await page.WaitForFunctionAsync("() => window.Blazor?.addEventListener");
        await page.EvaluateAsync("() => Blazor.addEventListener('enhancedload', () => window.sawEnhancedLoad = true)");
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Pages" }).GetByRole(AriaRole.Link, new() { Name = "Script" }).ClickAsync();
        await page.WaitForFunctionAsync("() => window.sawEnhancedLoad");
        await CheckPopoverAsync(page);
        await CheckCheckboxAsync(page);
        await CheckNumberAsync(page, false);
    }

    [Theory]
    [ModeData(RenderMode.Server)]
    public async Task InteractiveReplacementGetsItsOwnMixedProperty(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenBlankAsync("");
        IPage page = sample.Page;
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync("**/_blazor/negotiate?*", async route =>
        {
            await resume.Task;
            await route.ContinueAsync();
        });
        try
        {
            await page.GotoAsync(new Uri(gallery.BaseAddress, $"{mode.Segment()}/script").AbsoluteUri);
            await Expect(page.Locator("main")).ToHaveAttributeAsync("data-interactive", "false");
            await Expect(page.Locator(Checkbox)).ToHaveJSPropertyAsync("indeterminate", true);
            await page.Locator(Checkbox).EvaluateAsync("el => { window.prerenderBox = el; }");
        }
        finally
        {
            resume.SetResult();
        }

        await Expect(page.Locator("main")).ToHaveAttributeAsync("data-interactive", "true");
        Assert.False(await page.Locator(Checkbox).EvaluateAsync<bool>("el => el === window.prerenderBox"));
        await Expect(page.Locator(Checkbox)).ToHaveJSPropertyAsync("indeterminate", true);
    }

    [Fact]
    public async Task PlainModuleEnhancesReplacedMarkupAndChangedAttributes()
    {
        const string html = """
            <!doctype html><meta charset="utf-8">
            <script type="module" src="_content/FluentCMS.Components/FluentCMS.Components.lib.module.js"></script>
            <main>
              <button id="script-trigger" popovertarget="script-popover" aria-expanded="false">Note</button>
              <div id="script-popover" class="fl-popover" popover>Note text</div>
              <input id="script-checkbox" class="fl-checkbox__control" type="checkbox" data-fl-indeterminate>
              <span class="fl-number-input">
                <input id="script-number" class="fl-number-input__control" type="number" value="1" min="1" step="0.5">
                <button class="fl-number-input__step" type="button">Minus</button><button class="fl-number-input__step" type="button">Plus</button>
              </span>
            </main>
            """;
        await using GalleryPage sample = await gallery.OpenBlankAsync("");
        IPage page = sample.Page;
        Uri url = new(gallery.BaseAddress, "static/plain.html");
        await page.RouteAsync(url.AbsoluteUri, route => route.FulfillAsync(new()
        {
            ContentType = "text/html",
            Headers = new Dictionary<string, string> { ["Content-Security-Policy"] = "default-src 'none'; script-src 'self'" },
            Body = html,
        }));
        await page.GotoAsync(url.AbsoluteUri);
        Assert.False(await page.EvaluateAsync<bool>("() => 'Blazor' in window"));
        ILocator checkbox = page.Locator(Checkbox);
        ILocator down = page.Locator(Step).First;
        ILocator up = page.Locator(Step).Last;
        await Expect(checkbox).ToHaveJSPropertyAsync("indeterminate", true);
        await Expect(down).ToBeDisabledAsync();

        await page.Locator("main").EvaluateAsync("el => el.outerHTML = el.outerHTML");
        await Expect(checkbox).ToHaveJSPropertyAsync("indeterminate", true);
        await CheckPopoverAsync(page);
        await checkbox.EvaluateAsync("el => el.removeAttribute('data-fl-indeterminate')");
        await Expect(checkbox).ToHaveJSPropertyAsync("indeterminate", false);
        await page.Locator(Number).EvaluateAsync("el => el.max = '1.75'");
        await up.ClickAsync();
        await Expect(page.Locator(Number)).ToHaveValueAsync("1.5");
        await Expect(up).ToBeDisabledAsync();
        await Expect(down).ToBeEnabledAsync();
        await page.Locator(Number).EvaluateAsync("el => el.max = '2'");
        await Expect(up).ToBeEnabledAsync();
    }

    [Fact(Explicit = true)]
    public async Task PublishedStandaloneRegistersTypedEvent()
    {
        string address = Environment.GetEnvironmentVariable("FLUENTCMS_PUBLISHED_URL")
            ?? throw new InvalidOperationException("Set FLUENTCMS_PUBLISHED_URL to the served standalone wwwroot.");
        await using GalleryPage sample = await gallery.OpenBlankAsync("");
        IPage page = sample.Page;
        await page.GotoAsync(address);
        await page.GetByRole(AriaRole.Navigation, new() { Name = "Pages" }).GetByRole(AriaRole.Link, new() { Name = "Script" }).ClickAsync();
        await CheckPopoverAsync(page);
        await CheckCheckboxAsync(page);
        await CheckNumberAsync(page, true);
    }

    [Theory]
    [ModeData(RenderMode.Static)]
    public async Task NumericLimitIgnoresValueSpelling(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "script");
        IPage page = sample.Page;
        await page.Locator(Number).EvaluateAsync("el => { el.value = '02'; el.dispatchEvent(new Event('input', { bubbles: true })); }");
        await Expect(page.Locator(Step).Last).ToBeDisabledAsync();
        await page.Locator(Number).EvaluateAsync("el => { el.value = ''; el.dispatchEvent(new Event('input', { bubbles: true })); }");
        await Expect(page.Locator(Step).First).ToBeEnabledAsync();
    }

    [Theory]
    [ModeData(RenderMode.Static)]
    public async Task NoBindingEventForNumericNoOp(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "script");
        IPage page = sample.Page;
        // A step can race the notification of a programmatic value write.
        await Expect(page.Locator(Checkbox)).ToHaveJSPropertyAsync("indeterminate", true);
        await page.Locator(Number).EvaluateAsync("""
            el => {
              el.value = '02';
              window.notified = 0;
              for (const type of ['change', 'input']) el.addEventListener(type, () => window.notified++);
            }
            """);
        await page.Locator(Step).Last.ClickAsync();
        Assert.Equal(0, await page.EvaluateAsync<int>("window.notified"));
    }

    [Theory]
    [ModeData(RenderMode.Static)]
    public async Task NativeResetRestoresNumberLimits(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "script");
        IPage page = sample.Page;
        await page.Locator(".fl-number-input").EvaluateAsync("box => { const form = document.createElement('form'); box.before(form); form.append(box); }");
        ILocator number = page.Locator(Number);
        ILocator up = page.Locator(Step).Last;
        await up.ClickAsync();
        await Expect(up).ToBeDisabledAsync();
        await number.EvaluateAsync("el => { el.form.addEventListener('reset', e => e.preventDefault(), { once: true }); el.form.reset(); }");
        await Expect(number).ToHaveValueAsync("2");
        await Expect(up).ToBeDisabledAsync();
        await number.EvaluateAsync("el => el.form.reset()");
        await Expect(number).ToHaveValueAsync("1");
        await Expect(up).ToBeEnabledAsync();
    }

    [Theory]
    [ModeData(RenderMode.Server, RenderMode.Wasm)]
    public async Task ModelWriteUpdatesLimitsOnRetainedInput(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "script");
        IPage page = sample.Page;
        ILocator up = page.Locator(Step).Last;
        ILocator number = page.Locator(Number);
        await number.EvaluateAsync("el => { window.retainedNumber = el; }");
        await up.ClickAsync();
        await Expect(page.Locator("#script-bound-value")).ToHaveTextAsync("2");
        await Expect(up).ToBeDisabledAsync();
        await page.Locator("#script-set-one").ClickAsync();
        await Expect(number).ToHaveValueAsync("1");
        Assert.True(await number.EvaluateAsync<bool>("el => el === window.retainedNumber"));
        await Expect(up).ToBeEnabledAsync();
    }

    [Theory]
    [ModeData(RenderMode.Static)]
    public async Task PanelReplacementAndRemovalUpdateRetainedTrigger(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "script");
        IPage page = sample.Page;
        ILocator trigger = page.Locator("#script-trigger");
        ILocator panel = page.Locator("#script-popover");
        await trigger.ClickAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await panel.EvaluateAsync("el => el.replaceWith(el.cloneNode(true))");
        await Expect(panel).ToBeHiddenAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await trigger.ClickAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await trigger.EvaluateAsync("el => { const copy = el.cloneNode(true); copy.id = 'later-trigger'; copy.removeAttribute('aria-expanded'); el.after(copy); }");
        ILocator later = page.Locator("#later-trigger");
        await Expect(later).ToHaveAttributeAsync("aria-expanded", "true");
        await panel.EvaluateAsync("el => el.remove()");
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(later).ToHaveAttributeAsync("aria-expanded", "false");
    }

    private static async Task CheckPopoverAsync(IPage page)
    {
        ILocator trigger = page.Locator("#script-trigger");
        ILocator popover = page.Locator("#script-popover");
        KeyboardDriver keys = page.Keys();
        await keys.StartAtTopAsync();
        await keys.TabToAsync("#script-trigger");
        await keys.PressAsync("Enter");
        await Expect(popover).ToBeVisibleAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await keys.PressAsync("Escape");
        await Expect(popover).ToBeHiddenAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    private static async Task CheckCheckboxAsync(IPage page)
    {
        ILocator box = page.Locator(Checkbox);
        await Expect(box).ToHaveJSPropertyAsync("indeterminate", true);
        KeyboardDriver keys = page.Keys();
        await keys.StartAtTopAsync();
        await keys.TabToAsync(Checkbox);
        await keys.PressAsync("Space");
        await Expect(box).ToBeCheckedAsync();
        await Expect(box).ToHaveJSPropertyAsync("indeterminate", false);
    }

    private static async Task CheckNumberAsync(IPage page, bool interactive)
    {
        ILocator number = page.Locator(Number);
        ILocator down = page.Locator(Step).First;
        ILocator up = page.Locator(Step).Last;
        await number.EvaluateAsync("""
            input => {
              window.stepEvents = [];
              for (const type of ['input', 'change']) {
                document.addEventListener(type, e => { if (e.target === input) window.stepEvents.push(e.type); });
              }
            }
            """);
        await up.ClickAsync();
        await Expect(number).ToHaveValueAsync("2");
        await Expect(up).ToBeDisabledAsync();
        Assert.Equal(["input", "change"], await page.EvaluateAsync<string[]>("window.stepEvents"));
        await Expect(page.Locator("#script-bound-value")).ToHaveTextAsync(interactive ? "2" : "1");
        if (interactive)
        {
            await Expect(page.Locator("#script-event-value")).ToHaveTextAsync("2");
        }

        await down.ClickAsync();
        await down.ClickAsync();
        await Expect(number).ToHaveValueAsync("0");
        await Expect(down).ToBeDisabledAsync();
        KeyboardDriver keys = page.Keys();
        await keys.StartAtTopAsync();
        await keys.TabToAsync(Number);
        await keys.PressAsync("ArrowUp");
        await Expect(number).ToHaveValueAsync("1");
        await Expect(down).ToBeEnabledAsync();
        await Expect(up).ToBeEnabledAsync();
    }
}
