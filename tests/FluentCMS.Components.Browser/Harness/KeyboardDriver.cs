using Microsoft.Playwright;

namespace FluentCMS.Components.Browser.Harness;

public sealed record FocusInfo(string Tag, string? Id, string? Role, string Snapshot)
{
    public override string ToString()
    {
        string prefix = $"- {Tag} ";
        string description = Snapshot.StartsWith(prefix, StringComparison.Ordinal) ? Snapshot[prefix.Length..] : Snapshot;
        return $"<{Tag}{(Id is null ? "" : "#" + Id)}{(Role is null ? "" : " role=" + Role)}> {description}";
    }
}

public static class KeyboardDriverExtensions
{
    public static KeyboardDriver Keys(this IPage page) => new(page);
}

// Uses the page with the keyboard alone and reads where focus is, so that a test of an ARIA
// pattern reads as the keys a person presses and the place focus must be after each.
//
//   KeyboardDriver keys = page.Keys();
//   await keys.TabToAsync("[role=tab]");
//   await keys.PressAsync("ArrowRight");
//   await keys.AssertFocusAsync("[role=tab][aria-selected=true]");
public sealed class KeyboardDriver(IPage page)
{
    private const float FocusTimeoutMs = 3000;
    private ILocator Focused => page.Locator(":light(:focus:not(html, body))");

    // Puts focus and the point where the next Tab starts at the top of the document. A page may
    // move focus when it loads (the gallery puts it on the heading), and browsers then differ
    // in where the first Tab goes.
    public async Task StartAtTopAsync() => await page.EvaluateAsync("""
        () => {
          const start = document.createElement('span');
          start.tabIndex = -1;
          document.body.prepend(start);
          start.focus();
          start.remove();
          scrollTo(0, 0);
        }
        """);

    public async Task PressAsync(string key) => await page.Keyboard.PressAsync(key);

    public async Task TabAsync() => await PressAsync("Tab");

    public async Task ShiftTabAsync() => await PressAsync("Shift+Tab");

    public async Task TypeAsync(string text) => await page.Keyboard.TypeAsync(text);

    // The element that has focus; fails when focus is on no element, which is on the page itself.
    public async Task<FocusInfo> FocusAsync() =>
        await ReadFocusAsync() ?? throw Fail("Focus is on no element.");

    public Task AssertFocusNameAsync(string name) =>
        Assertions.Expect(Focused).ToHaveAccessibleNameAsync(name, new() { Timeout = FocusTimeoutMs });

    // Presses Tab until focus is on an element that matches, and fails with the stops it passed.
    public async Task TabToAsync(string selector, int maxPresses = 60)
    {
        List<string> stops = [];
        for (int press = 0; press < maxPresses; press++)
        {
            await TabAsync();
            if (await page.EvaluateAsync<bool>("selector => !!document.activeElement?.matches(selector)", selector))
            {
                return;
            }

            stops.Add((await ReadFocusAsync())?.ToString() ?? "(nothing)");
        }

        Assert.Fail($"Tab did not reach '{selector}' in {maxPresses} presses. Focus went through:{Environment.NewLine}  {string.Join($"{Environment.NewLine}  ", stops)}");
    }

    // Sample a known part of the focus treatment, not the whole control (a caret or text may change).
    // The point is halfway down the indicator's left edge; a negative offset samples an outer ring.
    public async Task TabToAndAssertPaintChangeAsync(string selector, int xFromLeft, ILocator? indicator = null)
    {
        indicator ??= page.Locator(selector);
        string before = await ReadPixelAsync(indicator, xFromLeft);
        await TabToAsync(selector);
        string after = await ReadPixelAsync(indicator, xFromLeft);
        Assert.True(before != after, $"Tab reached '{selector}', but the indicator pixel on {indicator} at x={xFromLeft} stayed {after}.");
    }

    // Waits a moment for focus to settle, since a pattern may move it after the key press.
    public async Task AssertFocusAsync(string selector)
    {
        try
        {
            await page.WaitForFunctionAsync("selector => !!document.activeElement?.matches(selector)", selector, new PageWaitForFunctionOptions { Timeout = FocusTimeoutMs });
        }
        catch (TimeoutException)
        {
            Assert.Fail($"Focus should be on '{selector}' and is on {(await ReadFocusAsync())?.ToString() ?? "nothing"}.");
        }
    }

    private async Task<FocusInfo?> ReadFocusAsync()
    {
        string?[]? element = await Focused.EvaluateAllAsync<string?[]?>("([el]) => el ? [el.localName, el.id || null, el.getAttribute('role')] : null");
        if (element is null)
        {
            return null;
        }

        string snapshot = await Focused.AriaSnapshotAsync(new() { Depth = 1, Timeout = FocusTimeoutMs });
        return new FocusInfo(element[0]!, element[1], element[2], snapshot);
    }

    private async Task<string> ReadPixelAsync(ILocator indicator, int xFromLeft)
    {
        await indicator.ScrollIntoViewIfNeededAsync();
        var box = await indicator.BoundingBoxAsync() ?? throw Fail($"No visible box for {indicator}.");
        byte[] png = await page.ScreenshotAsync(new()
        {
            Clip = new() { X = MathF.Floor(box.X + xFromLeft), Y = MathF.Floor(box.Y + box.Height / 2), Width = 1, Height = 1 },
            Scale = ScreenshotScale.Css,
        });
        return await page.EvaluateAsync<string>("""
            async png => {
              const bytes = Uint8Array.from(atob(png), c => c.charCodeAt(0));
              const pixel = await createImageBitmap(new Blob([bytes], {type: 'image/png'}));
              const ctx = document.createElement('canvas').getContext('2d');
              ctx.drawImage(pixel, 0, 0);
              return '#' + Array.from(ctx.getImageData(0, 0, 1, 1).data, n => n.toString(16).padStart(2, '0')).join('');
            }
            """, Convert.ToBase64String(png));
    }

    private static Xunit.Sdk.XunitException Fail(string message) => new(message);
}
