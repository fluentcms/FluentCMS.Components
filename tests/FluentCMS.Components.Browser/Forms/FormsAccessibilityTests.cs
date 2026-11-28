using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FluentCMS.Components.Browser.Forms;

public sealed class FormsAccessibilityTests(GalleryFixture gallery)
{
    [Theory]
    [ModeData(Pages = ["field", "text-input"])]
    public Task GalleryPassesAxe(RenderMode mode, string page) =>
        Accessibility.AssertNoViolationsAsync(gallery, mode, page);

    [Theory]
    [ModeData(Pages = ["field", "text-input"])]
    public async Task BothDensitiesReflowAndKeepEveryControlAtLeast24By24(RenderMode mode, string path)
    {
        foreach (string density in new[] { "comfortable", "compact" })
        {
            await using GalleryPage sample = await gallery.OpenAsync(mode, path, new GalleryOptions { Density = density });
            IPage page = sample.Page;
            await Expect(page.Locator("html")).ToHaveAttributeAsync("data-fl-density", density);
            string[] small = await page.Locator("main :is(.fl-input__control, .fl-input__action)").EvaluateAllAsync<string[]>("""
                controls => controls.flatMap(control => {
                  if (getComputedStyle(control).display === 'none') return [];
                  const {width, height} = control.getBoundingClientRect();
                  return width >= 24 && height >= 24 ? [] : [`${control.outerHTML}: ${width} × ${height}`];
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
        await using GalleryPage sample = await gallery.OpenAsync(mode, "text-input");
        IPage page = sample.Page;
        await Expect(page.Locator("[data-force]")).ToHaveCountAsync(8);
        await Expect(page.Locator("#input-matrix [data-force]")).ToHaveCountAsync(8);
        await page.WaitForFunctionAsync("""
            () => {
              const [rest, hover, focus] = document.querySelectorAll('#input-matrix tbody tr:first-child .fl-input');
              const style = el => getComputedStyle(el);
              return style(rest).borderColor !== style(hover).borderColor && style(focus).outlineStyle !== 'none' && style(rest).outlineStyle === 'none';
            }
            """);
        Assert.True(await page.Locator("#parts .fl-input:has(> #input-clear)").EvaluateAsync<bool>("""
            box => {
              const control = box.querySelector('input');
              const paint = () => [getComputedStyle(box).borderColor, getComputedStyle(box).outlineStyle].join('|');
              const rest = paint();
              control.setAttribute('data-force', 'hover focus-visible');
              const outside = paint();
              control.removeAttribute('data-force');
              return outside === rest;
            }
            """), "The gallery's forced-state stylesheet also affected an input outside the state matrix.");
    }

    [Theory]
    [ModeData]
    public async Task ReducedMotionHoldsWhileAnActionIsPressed(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "text-input", new GalleryOptions { ReducedMotion = true });
        IPage page = sample.Page;
        ILocator show = page.Locator("#parts .fl-input:has(> #input-password) > button.fl-input__action");
        await show.ScrollIntoViewIfNeededAsync();
        await Wcag.AssertNoMotionAsync(page, async () =>
        {
            await show.HoverAsync();
            await page.Mouse.DownAsync();
            await page.Mouse.UpAsync();
            await Expect(show).ToHaveAttributeAsync("aria-pressed", "true");
        });
    }

    [Theory]
    [ModeData]
    public async Task ADisabledAffixStaysLegibleOnTheDisabledFill(RenderMode mode)
    {
        foreach (string theme in new[] { "light", "dark" })
        {
            await using GalleryPage sample = await gallery.OpenAsync(mode, "text-input", new GalleryOptions { Theme = theme, Density = "compact" });
            ILocator affix = sample.Page.Locator("#input-matrix tbody tr:nth-child(4) td:nth-of-type(6) .fl-input__affix");
            await Expect(affix).ToHaveTextAsync("https://");
            double ratio = await affix.EvaluateAsync<double>("""
                affix => {
                  const rgb = value => value.match(/[\d.]+/g).slice(0, 3).map(Number);
                  const lum = ([r, g, b]) => [r, g, b].map(c => c / 255).map(c => c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4)
                    .reduce((sum, c, i) => sum + c * [0.2126, 0.7152, 0.0722][i], 0);
                  if (getComputedStyle(affix).backgroundColor !== 'rgba(0, 0, 0, 0)') return 0;
                  const [a, b] = [lum(rgb(getComputedStyle(affix).color)), lum(rgb(getComputedStyle(affix.parentElement).backgroundColor))];
                  return (Math.max(a, b) + 0.05) / (Math.min(a, b) + 0.05);
                }
                """);
            Assert.True(ratio >= 4.5, $"The disabled affix has {ratio:0.00}:1 on its fill in {theme}.");
            Assert.True(await affix.EvaluateAsync<bool>("a => a.parentElement.querySelector('input').disabled"));
        }
    }

    [Theory]
    [ModeData]
    public async Task ForcedColoursKeepTheBoxTheFocusRingAndTheDisabledState(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "text-input");
        IPage page = sample.Page;
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });
        Assert.True(await page.EvaluateAsync<bool>("() => matchMedia('(forced-colors: active)').matches"));
        ILocator box = page.Locator("#parts .fl-input:has(> #input-clear)");
        await page.Locator("#input-clear").FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        await page.Keyboard.PressAsync("Tab");
        var paint = await box.EvaluateAsync<string[]>("""
            box => {
              const style = getComputedStyle(box);
              const disabled = getComputedStyle(document.querySelector('#input-matrix tbody tr:nth-child(2) td:nth-of-type(6) .fl-input'));
              const affix = getComputedStyle(document.querySelector('#input-matrix tbody tr:nth-child(4) td:nth-of-type(6) .fl-input__affix'));
              return [style.borderStyle, style.outlineStyle, style.outlineWidth, disabled.borderStyle, disabled.borderColor, disabled.color, affix.color];
            }
            """);
        Assert.Equal("solid", paint[0]);
        Assert.Equal("solid", paint[1]);
        Assert.Equal("2px", paint[2]);
        Assert.Equal("solid", paint[3]);
        Assert.Equal(paint[4], paint[5]);
        Assert.Equal(paint[5], paint[6]);
    }
}
