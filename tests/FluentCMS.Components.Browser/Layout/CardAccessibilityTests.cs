using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FluentCMS.Components.Browser.Layout;

public sealed class CardAccessibilityTests(GalleryFixture gallery)
{
    [Theory]
    [ModeData]
    public Task GalleryPassesAxe(RenderMode mode) =>
        Accessibility.AssertNoViolationsAsync(gallery, mode, "card");

    [Theory]
    [ModeData]
    public async Task BothDensitiesKeepEveryActionableTargetAtLeast24By24(RenderMode mode)
    {
        foreach (string density in new[] { "comfortable", "compact" })
        {
            await using GalleryPage sample = await gallery.OpenAsync(mode, "card", new GalleryOptions { Density = density });
            IPage page = sample.Page;
            await Expect(page.Locator("html")).ToHaveAttributeAsync("data-fl-density", density);
            await Expect(page.Locator("#card-members")).ToBeVisibleAsync();
            string[] small = await page.Locator("main .fl-card a, main .fl-card button").EvaluateAllAsync<string[]>("""
                targets => targets.flatMap(target => {
                  const {width, height} = target.getBoundingClientRect();
                  return width >= 24 && height >= 24 ? [] : [`${target.outerHTML}: ${width} x ${height}`];
                })
                """);
            Assert.Empty(small);
            await Wcag.AssertTargetSizesAsync(page);
        }
    }

    [Theory]
    [ModeData]
    public async Task OnlyTheStateMatrixForcesLinkStatesAndTheyLookDifferent(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "card");
        IPage page = sample.Page;
        await Expect(page.Locator("#card-states a.fl-card")).ToHaveCountAsync(4);
        await Expect(page.Locator("[data-force]")).ToHaveCountAsync(3);
        await page.WaitForFunctionAsync("""
            () => {
              const [rest, hover, focus, pressed] = document.querySelectorAll('#card-states a.fl-card');
              const fill = el => getComputedStyle(el).backgroundImage;
              return fill(rest) !== fill(hover) && fill(hover) !== fill(pressed) && getComputedStyle(focus).outlineStyle !== 'none';
            }
            """);
        Assert.True(await page.Locator("#card-link-projects").EvaluateAsync<bool>("""
            el => {
              const paint = () => getComputedStyle(el).backgroundImage + getComputedStyle(el).outlineStyle;
              const rest = paint();
              el.setAttribute('data-force', 'hover active focus-visible');
              const outside = paint();
              el.removeAttribute('data-force');
              return outside === rest;
            }
            """), "The forced-state rules also affected a linked card outside the matrix.");
    }

    [Theory]
    [ModeData]
    public async Task ForcedColoursKeepTheBorderAndTheFocusIndicator(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "card");
        IPage page = sample.Page;
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ForcedColors = ForcedColors.Active });
        ILocator card = page.Locator("#card-identity");
        await Expect(card).ToHaveCSSAsync("border-top-style", "solid");
        Assert.NotEqual("0px", await card.EvaluateAsync<string>("el => getComputedStyle(el).borderTopWidth"));
        ILocator link = page.Locator("#card-link-projects");
        await link.FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("Tab");
        Assert.NotEqual("none", await link.EvaluateAsync<string>("el => getComputedStyle(el).outlineStyle"));
    }

    [Theory]
    [ModeData]
    public async Task ReducedMotionHoldsWhileALinkedCardIsHoveredAndPressed(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "card", new GalleryOptions { ReducedMotion = true });
        IPage page = sample.Page;
        ILocator card = page.Locator("#card-link-projects");
        await card.ScrollIntoViewIfNeededAsync();
        var resting = await card.BoundingBoxAsync();
        Assert.NotNull(resting);
        await Wcag.AssertNoMotionAsync(page, async () =>
        {
            await card.HoverAsync();
            await page.Mouse.DownAsync();
            try
            {
                await Expect(page.Locator("#card-link-projects:hover:active")).ToBeVisibleAsync();
                var pressed = await card.BoundingBoxAsync();
                Assert.NotNull(pressed);
                Assert.Equal((resting.X, resting.Y, resting.Width, resting.Height), (pressed.X, pressed.Y, pressed.Width, pressed.Height));
            }
            finally
            {
                await page.Mouse.UpAsync();
            }
        });
    }
}
