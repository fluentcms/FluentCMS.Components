using FluentCMS.Components.Browser.Harness;
using Xunit.Sdk;

namespace FluentCMS.Components.Browser.Samples;

// Each helper must fail on a fault. The pages here are set by the test itself, so the gallery
// holds no fault. These run once for a browser, in no render mode.
public sealed class HelperFaultTests(GalleryFixture gallery)
{
    private const string Head = "<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width'><title>Fault</title></head>";

    [Fact]
    public async Task AxeFailsOnAButtonWithoutAName()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync($"{Head}<body><main><h1>Fault</h1><button></button></main></body></html>");

        XunitException failure = await Assert.ThrowsAnyAsync<XunitException>(() => Accessibility.AssertNoViolationsAsync(page.Page));

        Assert.Contains("button-name", failure.Message);
        Assert.Contains("button", failure.Message);
        Assert.Contains("https://dequeuniversity.com/rules/axe/", failure.Message);
    }

    // The same faults put into a real page of the gallery, in the three modes, by script.
    [Theory]
    [ModeData]
    public async Task HelpersSeeAFaultInjectedIntoAGalleryPage(RenderMode mode)
    {
        await using GalleryPage page = await gallery.OpenAsync(mode, "tokens");
        await page.Page.EvaluateAsync("""
            () => document.querySelector('main').insertAdjacentHTML('beforeend',
              '<button id="fault-a" style="width:20px;height:20px;padding:0"></button><button id="fault-b" style="width:20px;height:20px;padding:0"></button>')
            """);

        XunitException axe = await Assert.ThrowsAnyAsync<XunitException>(() => Accessibility.AssertNoViolationsAsync(page.Page));
        XunitException size = await Assert.ThrowsAnyAsync<XunitException>(() => Wcag.AssertTargetSizesAsync(page.Page));

        Assert.Contains("button-name", axe.Message);
        Assert.Contains("fault-a", size.Message);
    }

    [Fact]
    public async Task AxePassesACleanPage()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync($"{Head}<body><main><h1>Clean</h1><button>Save</button></main></body></html>");

        await Accessibility.AssertNoViolationsAsync(page.Page);
    }

    [Fact]
    public async Task TargetSizeFailsOnTwoTargetsOf20Pixels()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync(
            $"{Head}<body><button id='a' style='width:20px;height:20px;padding:0'>A</button><button id='b' style='width:20px;height:20px;padding:0'>B</button></body></html>");

        XunitException failure = await Assert.ThrowsAnyAsync<XunitException>(() => Wcag.AssertTargetSizesAsync(page.Page));

        Assert.Contains("button#a", failure.Message);
        Assert.Contains("20.0 by 20.0 px", failure.Message);
    }

    [Fact]
    public async Task TargetSizeRejectsAnIsolatedSmallTargetAndAnInlineLink()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync(
            $"{Head}<body><button id='alone' style='width:20px;height:20px;padding:0;margin:40px'>A</button><p>Read the <a id='guide' href='#x'>guide</a> first.</p></body></html>");

        XunitException failure = await Assert.ThrowsAnyAsync<XunitException>(() => Wcag.AssertTargetSizesAsync(page.Page));

        Assert.Contains("button#alone", failure.Message);
        Assert.Contains("a#guide", failure.Message);
    }

    [Fact]
    public async Task TargetSizeRejectsTouchingThinButtons()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync(Head + """
            <body style="display:flex"><button id="short" style="width:10px;height:10px;padding:0">A</button>
            <button id="long" style="width:100px;height:10px;padding:0">B</button></body></html>
            """);

        XunitException failure = await Assert.ThrowsAnyAsync<XunitException>(() => Wcag.AssertTargetSizesAsync(page.Page));

        Assert.Contains("button#short", failure.Message);
        Assert.Contains("button#long", failure.Message);
    }

    [Fact]
    public async Task TargetSizeDoesNotCountTheGapBetweenCheckboxesAndTheirLabels()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync(Head + """
            <body><style>main {display:flex} input {width:12px;height:12px;margin:0} label {display:block;width:40px;height:12px}</style>
            <main><input id="a" type="checkbox"><input id="b" type="checkbox"></main>
            <div style="margin:60px"><label for="a">One</label><label for="b">Two</label></div></body></html>
            """);

        XunitException failure = await Assert.ThrowsAnyAsync<XunitException>(() => Wcag.AssertTargetSizesAsync(page.Page));

        Assert.Contains("input#a", failure.Message);
        Assert.Contains("input#b", failure.Message);
    }

    [Fact]
    public async Task TargetSizeChecksTreeItemsOutsideTheTabOrder()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync(Head + """
            <body><div role="tree"><div role="treeitem" tabindex="0" style="height:24px">First</div>
            <div id="second" role="treeitem" tabindex="-1" onclick="this.focus()" style="height:12px">Second</div></div></body></html>
            """);

        XunitException failure = await Assert.ThrowsAnyAsync<XunitException>(() => Wcag.AssertTargetSizesAsync(page.Page));

        Assert.Contains("div#second", failure.Message);
    }

    [Fact]
    public async Task TargetSizeExcludesControlsDisabledByAFieldset()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync(Head + """
            <body><fieldset disabled style="display:flex"><button style="width:10px;height:10px;padding:0">A</button>
            <button style="width:10px;height:10px;padding:0">B</button></fieldset></body></html>
            """);

        await Wcag.AssertTargetSizesAsync(page.Page);
    }

    [Fact]
    public async Task TargetSizeAcceptsAnActual24PixelTargetOrAssociatedLabel()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync(Head + """
            <body><button style="width:24px;height:24px;padding:0">A</button><input id="opt" type="checkbox">
            <label for="opt" style="display:inline-block;width:24px;height:24px">B</label></body></html>
            """);

        await Wcag.AssertTargetSizesAsync(page.Page);
    }

    [Fact]
    public async Task ReflowFailsOnAPageThatScrollsSidewaysAt320()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync($"{Head}<body><div id='wide' style='width:600px'>Wide</div></body></html>");

        XunitException failure = await Assert.ThrowsAnyAsync<XunitException>(() => Wcag.AssertNoHorizontalScrollAsync(page.Page));

        Assert.Contains("div#wide", failure.Message);
    }

    [Fact]
    public async Task ReflowPassesAWideTableThatScrollsInsideItsOwnBox()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync(
            $"{Head}<body><div style='overflow-x:auto'><table style='width:600px'><tr><td>Wide</td></tr></table></div></body></html>");

        await Wcag.AssertNoHorizontalScrollAsync(page.Page);
    }

    [Fact]
    public async Task MotionFailsOnAnAnimationThatIgnoresReducedMotion()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync(
            $"{Head}<style>@keyframes slide {{ from {{ margin-left: 0 }} to {{ margin-left: 200px }} }} #mover {{ animation: slide 2s infinite alternate }}</style><body><div id='mover'>Moves</div></body></html>");

        XunitException failure = await Assert.ThrowsAnyAsync<XunitException>(() => Wcag.AssertNoMotionAsync(page.Page));

        Assert.Contains("div#mover", failure.Message);
    }

    [Fact]
    public async Task MotionPassesAnAnimationThatReducedMotionTurnsOff()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync(
            $"{Head}<style>@keyframes slide {{ from {{ margin-left: 0 }} to {{ margin-left: 200px }} }} @media (prefers-reduced-motion: no-preference) {{ #mover {{ animation: slide 2s infinite alternate }} }}</style><body><div id='mover'>Still</div></body></html>");

        await Wcag.AssertNoMotionAsync(page.Page);
    }

    [Fact]
    public async Task MotionFailsDuringATwoSecondHoverTransition()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync(Head + """
            <body><style>#marker {width:20px;transition:transform 2s linear} button:hover + #marker {transform:translateX(120px)}</style>
            <button>Hover me</button><div id="marker" ontransitionend="this.dataset.done='true'">→</div></body></html>
            """);

        XunitException failure = await Assert.ThrowsAnyAsync<XunitException>(() => Wcag.AssertNoMotionAsync(page.Page, async () =>
        {
            await page.Page.Locator("button").HoverAsync();
            await page.Page.Locator("#marker[data-done=true]").WaitForAsync();
        }));

        Assert.Contains("div#marker", failure.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MotionDistinguishesADrawerSlideFromAnInstantOpen(bool slide)
    {
        string transition = slide ? "transition:transform 400ms linear" : "";
        await using GalleryPage page = await gallery.OpenBlankAsync(Head + $$"""
            <body><style>aside {width:150px;transform:translateX(-180px);{{transition}}
            } aside.open {transform:none}</style>
            <button onclick="document.querySelector('aside').classList.add('open')">Open</button>
            <aside id="drawer" ontransitionend="this.dataset.done='true'">Drawer</aside></body></html>
            """);

        async Task OpenDrawerAsync()
        {
            await page.Page.Locator("button").ClickAsync();
            await page.Page.Locator(slide ? "#drawer.open[data-done=true]" : "#drawer.open").WaitForAsync();
            Assert.Equal("none", await page.Page.Locator("#drawer").EvaluateAsync<string>("el => getComputedStyle(el).transform"));
        }

        if (slide)
        {
            XunitException failure = await Assert.ThrowsAnyAsync<XunitException>(() => Wcag.AssertNoMotionAsync(page.Page, OpenDrawerAsync));
            Assert.Contains("aside#drawer", failure.Message);
        }
        else
        {
            await Wcag.AssertNoMotionAsync(page.Page, OpenDrawerAsync);
        }
    }

    [Fact]
    public async Task MotionPassesAStationaryColorAnimation()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync(Head + """
            <body><style>@keyframes tint {from {color:red} to {color:blue}} #still {animation:tint 2s infinite alternate}</style>
            <div id="still">Still</div></body></html>
            """);

        Assert.True(await page.Page.Locator("#still").EvaluateAsync<bool>("el => el.getAnimations().some(a => a.playState === 'running')"));
        await Wcag.AssertNoMotionAsync(page.Page);
    }

    [Fact]
    public async Task KeyboardFailsAndNamesTheStopsWhenTabCannotReachAnElement()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync(
            $"{Head}<body><button>First</button><button>Second</button><div id='target' onclick='void 0'>A mouse-only control</div></body></html>");
        KeyboardDriver keys = page.Page.Keys();

        XunitException failure = await Assert.ThrowsAnyAsync<XunitException>(() => keys.TabToAsync("#target", maxPresses: 5));

        Assert.Contains("did not reach '#target'", failure.Message);
        Assert.Contains("\"First\"", failure.Message);
        Assert.Contains("\"Second\"", failure.Message);
    }

    [Fact]
    public async Task KeyboardFailsWhenFocusIsNotWhereAPatternSaysItIs()
    {
        await using GalleryPage page = await gallery.OpenBlankAsync($"{Head}<body><button>First</button><button id='second'>Second</button></body></html>");
        KeyboardDriver keys = page.Page.Keys();

        await keys.TabAsync();

        XunitException failure = await Assert.ThrowsAnyAsync<XunitException>(() => keys.AssertFocusAsync("#second"));
        Assert.Contains("is on <button> \"First\"", failure.Message);
        await keys.TabAsync();
        await keys.AssertFocusAsync("#second");
    }
}
