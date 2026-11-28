using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FluentCMS.Components.Browser.Forms;

public sealed class FormsPrerenderTests(GalleryFixture gallery)
{
    [Theory]
    [ModeData(RenderMode.Server, RenderMode.Wasm, Pages = ["field", "text-input"])]
    public async Task FirstInteractiveFieldsAndInputsMatchThePrerenderTheyReplaced(RenderMode mode, string path)
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

        Capture before;
        try
        {
            Uri address = new(gallery.BaseAddress, $"{mode.Segment()}/{path}");
            Assert.True((await page.GotoAsync(address.AbsoluteUri, new() { WaitUntil = WaitUntilState.Commit }))?.Ok);
            await intercepted.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
            await Expect(page.Locator($"main[data-render-mode='{mode.Segment()}']")).ToHaveAttributeAsync("data-interactive", "false");
            await Expect(page.Locator("main .fl-input").Last).ToBeAttachedAsync();
            before = await page.EvaluateAsync<Capture>(Snapshot);
        }
        finally
        {
            resume.TrySetResult();
        }

        await Expect(page.Locator($"main[data-render-mode='{mode.Segment()}']")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30000 });
        Assert.True(await page.EvaluateAsync<bool>("async () => (await window.firstInteractiveForms).replaced"));
        Capture after = await page.EvaluateAsync<Capture>("async () => (await window.firstInteractiveForms).capture");

        // Every library-owned part, id, label, description, value, affix, action and host-supplied name is identical.
        Assert.True(before.Parts.Length > 20, $"Only {before.Parts.Length} fields and inputs were captured.");
        Assert.Equal(before.Parts, after.Parts);
        if (path == "field")
        {
            Assert.Contains(after.Parts, html => html.Contains("id=\"fl-slug\"", StringComparison.Ordinal) && html.Contains("name=\"Settings.Slug\"", StringComparison.Ordinal));
            Assert.Contains(after.Parts, html => html.Contains("name=\"title\"", StringComparison.Ordinal));

            // The framework default, kept apart: InputBase derives a name only where OperatingSystem.IsBrowser() is false.
            Assert.Equal(["field-default-name", "field-default-input-text"], before.Defaults.Select(d => d.Id));
            Assert.All(before.Defaults, d => Assert.Equal("_defaultName", d.Name));
            string? expected = mode == RenderMode.Wasm ? null : "_defaultName";
            Assert.All(after.Defaults, d => Assert.Equal(expected, d.Name));
            Assert.Equal(before.Defaults.Select(d => d.Rest), after.Defaults.Select(d => d.Rest));
        }
        else
        {
            Assert.Empty(before.Defaults);
            Assert.Contains(after.Parts, html => html.Contains("name=\"q\"", StringComparison.Ordinal));
        }
    }

    public sealed class DefaultName
    {
        public string Id { get; set; } = "";
        public string? Name { get; set; }
        public string Rest { get; set; } = "";
    }

    public sealed class Capture
    {
        public string[] Parts { get; set; } = [];
        public DefaultName[] Defaults { get; set; } = [];
    }

    private const string Snapshot = """
        () => {
          const clean = el => {
            const copy = el.cloneNode(true);
            for (const parent of [copy, ...copy.querySelectorAll('*')]) {
              for (const child of [...parent.childNodes]) {
                if (child.nodeType === Node.COMMENT_NODE && child.data === '!') child.remove();
              }
            }
            // Blazor sets an input's value as a property, so compare the live value, not the attribute.
            const live = el.matches('input') ? [el] : [...el.querySelectorAll('input')];
            (copy.matches('input') ? [copy] : [...copy.querySelectorAll('input')]).forEach((input, i) => {
              input.removeAttribute('value');
              input.setAttribute('data-live-value', live[i].value);
            });
            return copy;
          };
          const capture = () => ({
            parts: [...document.querySelectorAll('main .fl-field, main .fl-input')].filter(el => !el.closest('#names')).map(el => clean(el).outerHTML),
            defaults: [...document.querySelectorAll('#names input')].map(el => {
              const copy = clean(el);
              const name = copy.getAttribute('name');
              copy.removeAttribute('name');
              // InputText captures an ElementReference, which Blazor marks with a _bl_ attribute once interactive.
              if (el.id === 'field-default-input-text') {
                for (const attr of [...copy.attributes]) if (attr.name.startsWith('_bl_')) copy.removeAttribute(attr.name);
              }
              return { id: el.id, name, rest: copy.outerHTML };
            }),
          });
          const original = [...document.querySelectorAll('main *')];
          window.firstInteractiveForms = new Promise(resolve => {
            const observer = new MutationObserver(() => {
              if (!document.querySelector('main[data-interactive="true"]')) return;
              observer.disconnect();
              resolve({ replaced: original.every(el => !el.isConnected), capture: capture() });
            });
            observer.observe(document.body, { subtree: true, childList: true, attributes: true });
          });
          return capture();
        }
        """;
}