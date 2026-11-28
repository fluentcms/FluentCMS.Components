using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FluentCMS.Components.Browser.Foundations;

public sealed class ButtonAccessibilityTests(GalleryFixture gallery)
{
    [Theory]
    [ModeData]
    public Task GalleryPassesAxe(RenderMode mode) =>
        Accessibility.AssertNoViolationsAsync(gallery, mode, "button");

    [Theory]
    [ModeData]
    public async Task BothDensitiesReflowAndKeepEveryButtonAtLeast24By24(RenderMode mode)
    {
        foreach (string density in new[] { "comfortable", "compact" })
        {
            await using GalleryPage sample = await gallery.OpenAsync(mode, "button", new GalleryOptions { Density = density });
            IPage page = sample.Page;
            await Expect(page.Locator("html")).ToHaveAttributeAsync("data-fl-density", density);
            await Expect(page.Locator("#button-action")).ToBeVisibleAsync();
            string[] small = await page.Locator("main .fl-button").EvaluateAllAsync<string[]>("""
                buttons => buttons.flatMap(button => {
                  const {width, height} = button.getBoundingClientRect();
                  return width >= 24 && height >= 24 ? [] : [`${button.outerHTML}: ${width} × ${height}`];
                })
                """);
            Assert.Empty(small);
            await Wcag.AssertTargetSizesAsync(page);
            await Wcag.AssertNoHorizontalScrollAsync(page);
        }
    }

    [Theory]
    [ModeData]
    public async Task OnlyMatrixSpecimensForceTheirInteractionStates(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "button");
        IPage page = sample.Page;
        ILocator rows = page.Locator("#button-matrix tbody > tr");
        await Expect(rows).ToHaveCountAsync(6);
        await Expect(page.Locator("#button-matrix button.fl-button")).ToHaveCountAsync(38);
        await Expect(page.Locator("[data-force]")).ToHaveCountAsync(18);
        foreach (ILocator row in await rows.AllAsync())
        {
            ILocator cells = row.Locator("td");
            await Expect(cells).ToHaveCountAsync(7);
            await Expect(cells.Nth(1).Locator("button")).ToHaveAttributeAsync("data-force", "hover");
            await Expect(cells.Nth(2).Locator("button")).ToHaveAttributeAsync("data-force", "focus-visible");
            await Expect(cells.Nth(3).Locator("button")).ToHaveAttributeAsync("data-force", "hover active");
        }

        // A data attribute without its gallery stylesheet twin would leave three default-looking cells.
        await page.WaitForFunctionAsync("""
            () => {
              const [rest, hover, focus, active] = document.querySelectorAll('#button-matrix tbody tr:first-child button');
              const fill = el => getComputedStyle(el).backgroundImage;
              return fill(rest) !== fill(hover) && fill(hover) !== fill(active) && getComputedStyle(focus).outlineStyle !== 'none';
            }
            """);
        Assert.True(await page.Locator("#button-action").EvaluateAsync<bool>("""
            el => {
              const paint = () => [getComputedStyle(el).backgroundImage, getComputedStyle(el).outlineStyle].join('|');
              const rest = paint();
              el.setAttribute('data-force', 'hover active focus-visible');
              const outsideMatrix = paint();
              el.removeAttribute('data-force');
              return outsideMatrix === rest;
            }
            """), "The gallery's forced-state stylesheet also affected a button outside the state matrix.");
    }

    [Theory]
    [ModeData]
    public async Task IconOnlyHasANameWithoutExposingItsDecorativeSvg(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "button");
        ILocator button = sample.Page.Locator("#icons button.fl-button--icon.fl-button--s");
        await Expect(button).ToHaveRoleAsync(AriaRole.Button);
        await Expect(button).ToHaveAccessibleNameAsync("Edit");
        await Expect(button.Locator("svg.fl-icon")).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [Theory]
    [ModeData]
    public async Task ReducedMotionHoldsDuringHoverAndPress(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "button", new GalleryOptions { ReducedMotion = true });
        IPage page = sample.Page;
        ILocator button = page.Locator("#button-action");
        await button.ScrollIntoViewIfNeededAsync();
        var resting = await button.BoundingBoxAsync();
        Assert.NotNull(resting);
        await Wcag.AssertNoMotionAsync(page, async () =>
        {
            await button.HoverAsync();
            await page.Mouse.DownAsync();
            try
            {
                await Expect(page.Locator("#button-action:hover:active")).ToBeVisibleAsync();
                var pressed = await button.BoundingBoxAsync();
                Assert.NotNull(pressed);
                Assert.Equal((resting.X, resting.Y, resting.Width, resting.Height), (pressed.X, pressed.Y, pressed.Width, pressed.Height));
            }
            finally
            {
                await page.Mouse.UpAsync();
            }

            await Expect(page.Locator("#button-count")).ToHaveTextAsync(mode.IsInteractive() ? "1" : "0");
        });
    }
}
