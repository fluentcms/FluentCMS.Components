using System.Text.Json;
using Microsoft.Playwright;

namespace FluentCMS.Components.Browser.Harness;

public sealed record FocusInfo(string Tag, string? Id, string? Role, string Name, bool HasIndicator)
{
    public override string ToString() => $"<{Tag}{(Id is null ? "" : "#" + Id)}{(Role is null ? "" : " role=" + Role)}> \"{Name}\"";
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

    // Playwright cannot build a record from a script result, so the script returns JSON.
    private async Task<FocusInfo?> ReadFocusAsync()
    {
        string? json = await page.EvaluateAsync<string?>(Describe);
        return json is null ? null : JsonSerializer.Deserialize<FocusInfo>(json, JsonSerializerOptions.Web);
    }

    private static Xunit.Sdk.XunitException Fail(string message) => new(message);

    private const string Describe = """
        () => {
          const el = document.activeElement;
          if (!el || el === document.body || el === document.documentElement) return null;
          const text = node => (node?.textContent ?? '').replace(/\s+/g, ' ').trim();
          const labelledBy = el.getAttribute('aria-labelledby');
          const name = el.getAttribute('aria-label')
            || (labelledBy ? labelledBy.split(/\s+/).map(id => text(document.getElementById(id))).join(' ') : '')
            || (el.labels?.length ? text(el.labels[0]) : '')
            || el.getAttribute('alt')
            || (el.matches('input[type=button],input[type=submit]') ? el.value : '')
            || text(el)
            || el.getAttribute('title')
            || '';
          const style = getComputedStyle(el);
          const outline = style.outlineStyle !== 'none' && parseFloat(style.outlineWidth) > 0;
          return JSON.stringify({
            tag: el.localName,
            id: el.id || null,
            role: el.getAttribute('role'),
            name: name.slice(0, 60),
            hasIndicator: outline || style.boxShadow !== 'none',
          });
        }
        """;
}
