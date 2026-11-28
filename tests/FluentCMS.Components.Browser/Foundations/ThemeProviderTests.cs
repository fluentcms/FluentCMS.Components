using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FluentCMS.Components.Browser.Foundations;

public sealed class ThemeProviderTests(GalleryFixture gallery)
{
    private const string Path = "theme-provider";
    private const string PageAction = "#theme-page-action";
    private const string OuterAction = "#theme-outer-action";
    private const string InnerAction = "#theme-inner-action";
    private static readonly string[] InheritedAttributes = ["class", "id"];

    [Theory]
    [ModeData]
    public async Task NestedSettingsApplyAndLiveChangesDoNotAffectSiblingsOrThePage(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, Path,
            new() { Theme = "dark", Direction = "rtl", Density = "compact" });
        IPage page = sample.Page;
        ILocator root = page.Locator("html");
        ILocator inner = page.Locator("#theme-inner");
        await Expect(inner).ToHaveClassAsync("gallery-theme-panel gallery-theme-plum");
        await Expect(inner).ToHaveAttributeAsync("data-fl-theme", "light");
        await Expect(inner).ToHaveAttributeAsync("data-fl-density", "comfortable");
        await Expect(inner).ToHaveAttributeAsync("dir", "ltr");
        await Expect(inner).ToHaveAttributeAsync("data-fl-accent", "plum");

        string[] initialOuter = await AppearanceAsync(page, OuterAction);
        string[] initialPage = await AppearanceAsync(page, PageAction);
        string[] initialInner = await AppearanceAsync(page, InnerAction);
        AssertAppearance(initialPage, "dark", "rtl", "24", "rgb(47, 100, 245)", "rgb(255, 255, 255)");
        AssertAppearance(initialOuter, "dark", "rtl", "24", "rgb(27, 117, 106)", "rgb(247, 255, 253)");
        AssertAppearance(initialInner, "light", "ltr", "28", "rgb(118, 58, 144)", "rgb(252, 245, 255)");
        Assert.Equal(3, new[] { initialPage[5], initialOuter[5], initialInner[5] }.Distinct().Count());

        if (mode.IsInteractive())
        {
            ILocator change = page.Locator("#theme-change");
            await using IJSHandle original = await inner.EvaluateHandleAsync("el => el");
            await change.ClickAsync();
            await Expect(page.Locator("#theme-state")).ToHaveTextAsync("Inheriting outer settings");
            Assert.Equal(InheritedAttributes, await inner.EvaluateAsync<string[]>("el => el.getAttributeNames().sort()"));
            Assert.Equal(initialOuter, await AppearanceAsync(page, InnerAction));
            await AssertUnchangedAsync();

            await change.ClickAsync();
            await Expect(page.Locator("#theme-state")).ToHaveTextAsync("Using explicit settings");
            Assert.Equal(initialInner, await AppearanceAsync(page, InnerAction));
            Assert.True(await inner.EvaluateAsync<bool>("(el, original) => el === original", original));
        }

        await AssertUnchangedAsync();

        async Task AssertUnchangedAsync()
        {
            Assert.Equal(initialPage, await AppearanceAsync(page, PageAction));
            Assert.Equal(initialOuter, await AppearanceAsync(page, OuterAction));
            await Expect(root).ToHaveAttributeAsync("data-fl-theme", "dark");
            await Expect(root).ToHaveAttributeAsync("data-fl-density", "compact");
            await Expect(root).ToHaveAttributeAsync("dir", "rtl");
            Assert.Null(await root.GetAttributeAsync("data-fl-accent"));
        }
    }

    [Theory]
    [ModeData]
    public async Task FocusUsesEachScopeAndTheProviderAddsNoTabStop(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, Path);
        IPage page = sample.Page;
        KeyboardDriver keys = page.Keys();
        await keys.StartAtTopAsync();
        await keys.TabToAndAssertPaintChangeAsync(PageAction, -3);
        List<string> focusColors = [await FocusMatchesScopeAsync(page.Locator(PageAction))];
        foreach (string selector in new[] { OuterAction, InnerAction })
        {
            await keys.TabAsync();
            await keys.AssertFocusAsync(selector);
            await keys.AssertFocusNameAsync("View example");
            focusColors.Add(await FocusMatchesScopeAsync(page.Locator(selector)));
        }
        Assert.Equal(3, focusColors.Distinct().Count());
        await keys.TabAsync();
        await keys.AssertFocusAsync("#theme-change");
        await keys.PressAsync("Space");
        await Expect(page.Locator("#theme-state")).ToHaveTextAsync(mode.IsInteractive() ? "Inheriting outer settings" : "Using explicit settings");
        await keys.AssertFocusAsync("#theme-change");
    }

    [Theory]
    [ModeData(RenderMode.Static)]
    public async Task NativeLinksWorkWithScriptBlocked(RenderMode mode)
    {
        await using IBrowserContext context = await gallery.Browser.NewContextAsync(new() { JavaScriptEnabled = false });
        IPage page = await context.NewPageAsync();
        var address = new Uri(gallery.BaseAddress, $"{mode.Segment()}/{Path}");
        Assert.True((await page.GotoAsync(address.AbsoluteUri))?.Ok);
        await page.Keys().TabToAsync(InnerAction);
        await page.Keyboard.PressAsync("Enter");
        await Expect(page).ToHaveURLAsync(new Uri(address, "#copy-example").AbsoluteUri);
    }

    [Theory]
    [ModeData]
    public Task GalleryPassesAxe(RenderMode mode) => Accessibility.AssertNoViolationsAsync(gallery, mode, Path);

    [Theory]
    [ModeData]
    public async Task BothDensitiesKeepTargetsAndReflowAt320(RenderMode mode)
    {
        foreach (string density in new[] { "comfortable", "compact" })
        {
            await using GalleryPage sample = await gallery.OpenAsync(mode, Path, new() { Density = density, Width = 320 });
            await Wcag.AssertTargetSizesAsync(sample.Page);
            await Wcag.AssertNoHorizontalScrollAsync(sample.Page);
        }
    }

    [Theory]
    [ModeData]
    public async Task ForcedColorsKeepVisibleFocusAndReducedMotionHoldsDuringAnAction(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, Path);
        IPage page = sample.Page;
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });
        Assert.True(await page.EvaluateAsync<bool>("matchMedia('(forced-colors: active)').matches"));
        await page.Keys().StartAtTopAsync();
        await page.Keys().TabToAndAssertPaintChangeAsync(InnerAction, -3);
        await Expect(page.Locator(InnerAction)).ToBeVisibleAsync();
        if (mode.IsInteractive())
        {
            await page.Keys().TabAsync();
            await page.Keys().AssertFocusAsync("#theme-change");
        }

        var address = new Uri(page.Url);
        await Wcag.AssertNoMotionAsync(page, async () =>
        {
            await page.Keys().PressAsync("Enter");
            if (mode.IsInteractive())
            {
                await Expect(page.Locator("#theme-state")).ToHaveTextAsync("Inheriting outer settings");
            }
            else
            {
                await Expect(page).ToHaveURLAsync(new Uri(address, "#copy-example").AbsoluteUri);
            }
        });
    }

    [Theory]
    [ModeData(RenderMode.Server, RenderMode.Wasm)]
    public async Task FirstInteractiveScopeMatchesThePrerenderItReplaced(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenBlankAsync("");
        IPage page = sample.Page;
        var intercepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync("**/_framework/blazor.web*.js", async route =>
        {
            intercepted.TrySetResult();
            await resume.Task;
            await route.ContinueAsync();
        });

        string before;
        ILocator main = page.Locator($"main[data-render-mode='{mode.Segment()}']");
        try
        {
            Uri address = new(gallery.BaseAddress, $"{mode.Segment()}/{Path}");
            Assert.True((await page.GotoAsync(address.AbsoluteUri, new() { WaitUntil = WaitUntilState.Commit }))?.Ok);
            await intercepted.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
            await Expect(main).ToHaveAttributeAsync("data-interactive", "false");
            await Expect(page.Locator(InnerAction)).ToBeAttachedAsync();
            before = await page.Locator("#theme-samples").EvaluateAsync<string>("""
                original => {
                  const snapshot = el => {
                    const copy = el.cloneNode(true);
                    const walker = document.createTreeWalker(copy, NodeFilter.SHOW_COMMENT);
                    const comments = [];
                    while (walker.nextNode()) if (walker.currentNode.data === '!') comments.push(walker.currentNode);
                    comments.forEach(node => node.remove());
                    return copy.outerHTML;
                  };
                  const previousNodes = [original, ...original.querySelectorAll('*')];
                  window.firstInteractiveTheme = new Promise(resolve => {
                    const observer = new MutationObserver(() => {
                      if (!document.querySelector('main[data-interactive="true"]')) return;
                      observer.disconnect();
                      resolve({replaced: previousNodes.every(node => !node.isConnected), markup: snapshot(document.querySelector('#theme-samples'))});
                    });
                    observer.observe(document.body, {subtree: true, childList: true, attributes: true});
                  });
                  return snapshot(original);
                }
                """);
        }
        finally
        {
            resume.TrySetResult();
        }

        await Expect(main).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30000 });
        Assert.True(await page.EvaluateAsync<bool>("async () => (await window.firstInteractiveTheme).replaced"));
        Assert.Equal(before, await page.EvaluateAsync<string>("async () => (await window.firstInteractiveTheme).markup"));
    }

    private static Task<string[]> AppearanceAsync(IPage page, string selector) => page.Locator(selector).EvaluateAsync<string[]>("""
        el => {
          const css = getComputedStyle(el);
          return [css.colorScheme, css.direction, String(el.getBoundingClientRect().height), css.backgroundColor, css.color,
            css.getPropertyValue('--fl-color-accent-hover')];
        }
        """);

    private static void AssertAppearance(string[] actual, params string[] expected) => Assert.Equal(expected, actual[..5]);

    private static async Task<string> FocusMatchesScopeAsync(ILocator link)
    {
        string[] colors = await link.EvaluateAsync<string[]>("""
            link => {
              const probe = document.createElement('span');
              probe.style.color = 'var(--fl-color-focus)';
              link.append(probe);
              const colors = [getComputedStyle(probe).color, getComputedStyle(link).outlineColor];
              probe.remove();
              return colors;
            }
            """);
        Assert.Equal(colors[0], colors[1]);
        return colors[1];
    }
}
