using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FluentCMS.Components.Browser.Foundations;

public sealed class ButtonPrerenderTests(GalleryFixture gallery)
{
    [Theory]
    [ModeData(RenderMode.Server, RenderMode.Wasm)]
    public async Task FirstInteractiveButtonTreesMatchThePrerenderTheyReplaced(RenderMode mode)
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
        try
        {
            Uri address = new(gallery.BaseAddress, $"{mode.Segment()}/button");
            Assert.True((await page.GotoAsync(address.AbsoluteUri, new() { WaitUntil = WaitUntilState.Commit }))?.Ok);
            await intercepted.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
            await Expect(page.Locator($"main[data-render-mode='{mode.Segment()}']")).ToHaveAttributeAsync("data-interactive", "false");
            await Expect(page.Locator("button#button-action > svg.fl-icon path").First).ToBeAttachedAsync();

            before = await page.EvaluateAsync<string[]>("""
                () => {
                  const buttons = () => [...document.querySelectorAll('main .fl-button')];
                  const snapshot = els => els.map(el => {
                    const copy = el.cloneNode(true);
                    // Strip only Blazor's <!--!--> comment nodes, never text or attribute values.
                    for (const parent of [copy, ...copy.querySelectorAll('*')]) {
                      for (const child of [...parent.childNodes]) {
                        if (child.nodeType === Node.COMMENT_NODE && child.data === '!') child.remove();
                      }
                    }
                    return copy.outerHTML;
                  });
                  const original = buttons();
                  const oldElements = original.flatMap(el => [el, ...el.querySelectorAll('*')]);
                  window.firstInteractiveButtons = new Promise(resolve => {
                    const observer = new MutationObserver(() => {
                      if (!document.querySelector('main[data-interactive="true"]')) return;
                      observer.disconnect();
                      resolve({replaced: oldElements.every(el => !el.isConnected), markup: snapshot(buttons())});
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

        await Expect(page.Locator($"main[data-render-mode='{mode.Segment()}']")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30000 });
        Assert.True(await page.EvaluateAsync<bool>("async () => (await window.firstInteractiveButtons).replaced"));
        string[] after = await page.EvaluateAsync<string[]>("async () => (await window.firstInteractiveButtons).markup");
        Assert.Equal(before, after);
    }
}
