using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;
using System.Web;
using static Microsoft.Playwright.Assertions;

namespace FluentCMS.Components.Browser.Foundations;

public sealed class PatternTests(GalleryFixture gallery)
{
    private const string Path = "patterns";

    [Theory]
    [ModeData]
    public async Task BothDensitiesKeepProseLinksUnderlinedAndAtLeast24By24(RenderMode mode)
    {
        foreach (string density in new[] { "comfortable", "compact" })
        {
            await using GalleryPage sample = await gallery.OpenAsync(mode, Path, new() { Density = density, Width = 320 });
            ILocator targets = sample.Page.Locator("#pattern-prose .fl-link");
            await Expect(targets).ToHaveCountAsync(2);
            var geometry = await targets.EvaluateAllAsync<string[]>("""
                els => els.flatMap(el => {
                  const rects = [...el.getClientRects()];
                  return rects.some(r => r.width >= 24 && r.height >= 24) && getComputedStyle(el).textDecorationLine.includes('underline')
                    ? [] : [`${el.localName}: ${rects.map(r => `${r.width} x ${r.height}`).join(', ')}`];
                })
                """);
            Assert.True(geometry.Length == 0, $"{density}: {string.Join("; ", geometry)}");
        }
    }

    [Theory]
    [ModeData]
    public async Task ToolbarKeepsSearchAndEveryEndActionInsideAt320(RenderMode mode)
    {
        foreach (string density in new[] { "comfortable", "compact" })
        {
            await using GalleryPage sample = await gallery.OpenAsync(mode, Path, new() { Density = density, Width = 320 });
            ILocator toolbar = sample.Page.Locator("#pattern-search");
            await Expect(toolbar.Locator("input[type=search]")).ToBeVisibleAsync();
            await Expect(toolbar.Locator("button")).ToHaveCountAsync(3);
            string[] geometry = await toolbar.EvaluateAsync<string[]>("""
                el => {
                  const toolbar = el.getBoundingClientRect();
                  const controls = [...el.querySelectorAll('input[type=search], button')];
                  const boxes = controls.map(control => control.getBoundingClientRect());
                  return boxes.flatMap((r, i) => r.width >= 24 && r.height >= 24 && r.left >= toolbar.left && r.right <= toolbar.right
                    ? [] : [`${controls[i].textContent || controls[i].name}: [${r.left}, ${r.right}] outside toolbar [${toolbar.left}, ${toolbar.right}] or undersized (${r.width} x ${r.height})`]);
                }
                """);
            Assert.True(geometry.Length == 0, $"{density}: {string.Join("; ", geometry)}");
        }
    }

    [Theory]
    [ModeData]
    public async Task EachPatternRendersItsSemanticsAndReflows(RenderMode mode)
    {
        foreach (string density in new[] { "comfortable", "compact" })
        {
            await using GalleryPage sample = await gallery.OpenAsync(mode, Path,
                new() { Density = density, Theme = "dark", Direction = "rtl", Width = 320 });
            IPage page = sample.Page;
            await Expect(page.Locator("#stack .gallery-demo > .fl-stack")).ToHaveCSSAsync("flex-direction", "column");
            await Expect(page.Locator("#stack .gallery-demo > .fl-stack")).ToHaveCSSAsync("gap", density == "compact" ? "12px" : "16px");
            await Expect(page.Locator("#cluster ul.fl-cluster")).ToHaveRoleAsync(AriaRole.List);
            await Expect(page.Locator("#cluster ul.fl-cluster")).ToHaveCSSAsync("flex-wrap", "wrap");
            ILocator title = page.GetByRole(AriaRole.Heading, new() { Level = 3, NameRegex = new("publishing in multiple languages") });
            await Expect(title).ToHaveCSSAsync("text-overflow", "ellipsis");
            Assert.True(await title.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"));
            await Expect(page.GetByRole(AriaRole.Search, new() { Name = "Article filters" }).GetByRole(AriaRole.Searchbox)).ToHaveAccessibleNameAsync("Search articles");
            await Expect(page.Locator("#divider hr.fl-divider")).ToHaveRoleAsync(AriaRole.Separator);
            await Expect(page.Locator("#divider .fl-divider--vertical")).ToHaveAttributeAsync("aria-orientation", "vertical");
            await Expect(page.Locator("#kbd kbd.fl-kbd")).ToHaveCountAsync(2);
            await Expect(page.Locator("#skeleton [aria-busy=true] .fl-skeleton[aria-hidden=true]")).ToHaveCountAsync(3);
            await Expect(page.Locator("#description-list dl:first-of-type .fl-description-list__item > dt + dd")).ToHaveCountAsync(3);
            await Expect(page.Locator("#stat-tile a.fl-stat-tile")).ToHaveAccessibleNameAsync("Drafts waiting for you 7 3 updated today");
            await Expect(page.Locator("#timeline ol.fl-timeline").GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);
            await Expect(page.Locator("#timeline time[datetime]")).ToHaveCountAsync(2);
            await Wcag.AssertTargetSizesAsync(page);
            await Wcag.AssertNoHorizontalScrollAsync(page);
        }
    }

    [Theory]
    [ModeData]
    public Task GalleryPassesAxe(RenderMode mode) => Accessibility.AssertNoViolationsAsync(gallery, mode, Path);

    [Theory]
    [ModeData]
    public async Task KeyboardTraversesTheActionsAndFollowsTheProseLink(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, Path);
        IPage page = sample.Page;
        KeyboardDriver keys = page.Keys();
        await keys.StartAtTopAsync();
        await keys.TabToAsync("#pattern-query");
        await keys.AssertFocusNameAsync("Search articles");
        await keys.TypeAsync("publication");
        await keys.TabAsync();
        await keys.AssertFocusNameAsync("Search all articles");
        await keys.TabAsync();
        await keys.AssertFocusNameAsync("Find unpublished drafts");
        await keys.TabAsync();
        await keys.AssertFocusNameAsync("Find scheduled articles");
        await keys.TabToAndAssertPaintChangeAsync("#pattern-prose a", -3);
        await keys.AssertFocusNameAsync("EN");
        await keys.TabAsync();
        await keys.AssertFocusNameAsync("reset the search");
        await keys.PressAsync("Space");
        await Expect(page.Locator("#pattern-query")).ToHaveValueAsync("");
        await keys.ShiftTabAsync();
        var address = new Uri(page.Url);
        await keys.PressAsync("Enter");
        await Expect(page).ToHaveURLAsync(new Uri(address, "#description-list").AbsoluteUri);
    }

    [Theory]
    [ModeData(RenderMode.Static)]
    public async Task NativeSearchSendsTheQueryAndPressedSubmitterWithoutJavaScript(RenderMode mode)
    {
        await using IBrowserContext context = await gallery.Browser.NewContextAsync(new() { JavaScriptEnabled = false });
        IPage page = await context.NewPageAsync();
        var address = new Uri(gallery.BaseAddress, $"{mode.Segment()}/{Path}?theme=dark&density=compact&dir=rtl");
        Assert.True((await page.GotoAsync(address.AbsoluteUri))?.Ok);
        ILocator field = page.GetByRole(AriaRole.Searchbox, new() { Name = "Search articles" });
        await field.PressAsync("Tab");
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keys().AssertFocusAsync("#pattern-query");
        await page.Keyboard.TypeAsync("spring & summer");
        await page.Keyboard.PressAsync("Tab");
        await page.Keyboard.PressAsync("Tab");
        await page.Keys().AssertFocusNameAsync("Find unpublished drafts");
        Task<IRequest> submission = page.WaitForRequestAsync(request => request.IsNavigationRequest && request.Url.Contains("q=", StringComparison.Ordinal));
        await page.Keyboard.PressAsync("Enter");
        IRequest request = await submission;
        Assert.Equal("GET", request.Method);
        Uri target = new(request.Url);
        Assert.Equal(address.AbsolutePath, target.AbsolutePath);
        var query = HttpUtility.ParseQueryString(target.Query);
        Assert.Equal("spring & summer", query["q"]);
        Assert.Equal("draft", query["view"]);
        await Expect(page.Locator("#pattern-search-result")).ToHaveTextAsync("Search: spring & summer. View: draft.");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-fl-theme", "dark");
        await Expect(page.Locator("html")).ToHaveAttributeAsync("data-fl-density", "compact");
    }

    [Theory]
    [ModeData]
    public async Task SkeletonStopsPulsingInReducedMotionAndHasAnEdgeInForcedColors(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, Path);
        IPage page = sample.Page;
        ILocator skeleton = page.Locator("#skeleton .fl-skeleton--block");
        await Expect(skeleton).ToHaveCSSAsync("animation-name", "fl-skeleton-pulse");
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active, ReducedMotion = ReducedMotion.Reduce });
        Assert.True(await page.EvaluateAsync<bool>("matchMedia('(forced-colors: active)').matches"));
        await Expect(skeleton).ToHaveCSSAsync("animation-name", "none");
        await Expect(skeleton).ToHaveCSSAsync("opacity", "1");
        Assert.True(await skeleton.EvaluateAsync<bool>("""
            el => { const css = getComputedStyle(el);
              return css.borderTopStyle === 'solid' && parseFloat(css.borderTopWidth) > 0 && css.borderTopColor !== css.backgroundColor;
            }
            """));
        KeyboardDriver keys = page.Keys();
        await keys.StartAtTopAsync();
        await keys.TabToAsync("#pattern-query");
        await keys.TypeAsync("spring");
        await keys.TabToAndAssertPaintChangeAsync("#pattern-prose button", -3);
        await Wcag.AssertNoMotionAsync(page, async () =>
        {
            await keys.PressAsync("Space");
            await Expect(page.Locator("#pattern-query")).ToHaveValueAsync("");
        });
    }

    [Theory]
    [ModeData(RenderMode.Server, RenderMode.Wasm)]
    public async Task FirstInteractiveSpecimensMatchThePrerenderTheyReplaced(RenderMode mode)
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

        string[] before;
        ILocator main = page.Locator($"main[data-render-mode='{mode.Segment()}']");
        try
        {
            var address = new Uri(gallery.BaseAddress, $"{mode.Segment()}/{Path}?theme=dark&density=compact&dir=rtl");
            Assert.True((await page.GotoAsync(address.AbsoluteUri, new() { WaitUntil = WaitUntilState.Commit }))?.Ok);
            await intercepted.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
            await Expect(main).ToHaveAttributeAsync("data-interactive", "false");
            await Expect(page.Locator("#timeline time")).ToHaveCountAsync(2);
            before = await main.EvaluateAsync<string[]>("""
                main => {
                  const specimens = el => [...el.querySelectorAll('.gallery-section .gallery-demo')];
                  const snapshot = list => list.map(el => {
                    const copy = el.cloneNode(true);
                    const walker = document.createTreeWalker(copy, NodeFilter.SHOW_COMMENT);
                    const comments = [];
                    while (walker.nextNode()) if (walker.currentNode.data === '!') comments.push(walker.currentNode);
                    comments.forEach(node => node.remove());
                    return copy.outerHTML;
                  });
                  const original = specimens(main);
                  window.firstInteractivePatterns = new Promise(resolve => {
                    const observer = new MutationObserver(() => {
                      const interactive = document.querySelector('main[data-interactive="true"]');
                      if (!interactive) return;
                      observer.disconnect();
                      resolve({replaced: original.every(el => !el.isConnected), markup: snapshot(specimens(interactive))});
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

        Assert.Equal(11, before.Length);
        await Expect(main).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30000 });
        Assert.True(await page.EvaluateAsync<bool>("async () => (await window.firstInteractivePatterns).replaced"));
        Assert.Equal(before, await page.EvaluateAsync<string[]>("async () => (await window.firstInteractivePatterns).markup"));
    }
}
