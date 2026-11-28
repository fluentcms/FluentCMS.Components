using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FluentCMS.Components.Browser.Layout;

public sealed class CardTests(GalleryFixture gallery)
{
    [Theory]
    [ModeData]
    public async Task SectionsAreNamedByTheirHeadingsAtTheLevelTheHostChose(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "card");
        IPage page = sample.Page;
        await Expect(page.Locator("main [data-level=static]")).ToBeVisibleAsync();

        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Identity" })).ToHaveCountAsync(1);
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Danger zone" })).ToHaveCountAsync(1);
        await Expect(page.GetByRole(AriaRole.Region, new() { Name = "Files" })).ToHaveCountAsync(1);
        await Expect(page.Locator("section#card-identity")).ToHaveAttributeAsync("aria-labelledby", "card-identity-title");
        await Expect(page.Locator("#card-identity-title")).ToHaveJSPropertyAsync("tagName", "H3");
        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Members", Level = 3 })).ToHaveCountAsync(1);
        await Expect(page.Locator("section.fl-card:has(header) > header > h3.fl-card__title")).ToHaveCountAsync(
            await page.Locator("section.fl-card > header").CountAsync());
    }

    [Theory]
    [ModeData]
    public async Task ALinkedCardHoldsNoOtherInteractiveElementAndIsOneKeyboardStop(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "card");
        IPage page = sample.Page;
        KeyboardDriver keys = page.Keys();

        Assert.Equal(0, await page.Locator("a.fl-card").EvaluateAllAsync<int>("""
            cards => cards.reduce((n, card) => n + card.querySelectorAll('a, button, input, select, textarea, summary, [tabindex], [role=button], [role=link]').length, 0)
            """));
        await Expect(page.Locator("#card-link-projects")).ToHaveAccessibleNameAsync("Projects Plan the work and follow it.");

        await keys.StartAtTopAsync();
        await keys.TabToAsync("#card-link-projects", maxPresses: 120);
        await keys.TabAsync();
        await keys.AssertFocusAsync("#card-link-members");
        await keys.TabAsync();
        await keys.AssertFocusAsync("#card-link-settings");
        await keys.ShiftTabAsync();
        await keys.ShiftTabAsync();
        await keys.AssertFocusAsync("#card-link-projects");
        await keys.PressAsync("Enter");
        await page.WaitForURLAsync("**/button*");
    }

    [Theory]
    [ModeData]
    public async Task DangerZoneIsAnOrdinaryCardWithAnExplainedDangerButton(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "card");
        IPage page = sample.Page;
        ILocator card = page.Locator("#card-danger");
        await Expect(card).ToHaveClassAsync("fl-card");
        ILocator button = card.GetByRole(AriaRole.Button, new() { Name = "Delete the site…" });
        await Expect(button).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bfl-button--danger\b"));
        await Expect(button).ToHaveAccessibleDescriptionAsync(new System.Text.RegularExpressions.Regex("cannot be undone"));
        Assert.Equal(0, await card.Locator("a, [class*='fl-card--']").CountAsync());
    }

    [Theory]
    [ModeData]
    public async Task FlushBodyReachesTheBorderAndIsClippedToTheCorners(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "card");
        ILocator body = sample.Page.Locator("#card-files > .fl-card__body--flush");
        await Expect(body).ToHaveCSSAsync("padding-top", "0px");
        await Expect(body).ToHaveCSSAsync("overflow", "hidden");
        Assert.NotEqual("0px", await body.EvaluateAsync<string>("el => getComputedStyle(el).borderStartStartRadius"));
    }

    [Theory]
    [ModeData]
    public async Task RowsAndActionsWrapAtThreeHundredTwentyPixelsInRightToLeft(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "card", new GalleryOptions { Direction = "rtl", Width = 320, Height = 640 });
        IPage page = sample.Page;
        await Expect(page.Locator("#card-members")).ToBeVisibleAsync();
        string[] overflowing = await page.Locator("main .fl-card").EvaluateAllAsync<string[]>("""
            cards => cards.filter(card => !card.closest('.gallery-scroll')).flatMap(card => {
              const root = document.documentElement;
              const box = card.getBoundingClientRect();
              return box.left < 0 || box.right > root.clientWidth ? [card.id] : [];
            })
            """);
        Assert.Empty(overflowing);
        await Wcag.AssertNoHorizontalScrollAsync(page);
    }
}
