using Microsoft.Playwright;

namespace FluentCMS.Components.Browser.Harness;

// Checks of WCAG 2.2 that axe-core does not make, on a page that is already open.
public static class Wcag
{
    private const int ReflowWidth = 320;

    // RECIPE §10 is stricter than WCAG 2.5.8: spacing and inline text do not excuse a small target.
    public static async Task AssertTargetSizesAsync(IPage page)
    {
        string[] findings = await page.EvaluateAsync<string[]>(TargetSizes);
        Fail(findings, "Targets under the project minimum of 24 by 24 CSS pixels");
    }

    // Success criterion 1.4.10, reflow: at 320 CSS pixels wide the page does not scroll sideways.
    // What scrolls inside a container of its own, such as a wide table, is not the page.
    public static async Task AssertNoHorizontalScrollAsync(IPage page)
    {
        PageViewportSizeResult? before = page.ViewportSize;
        await page.SetViewportSizeAsync(ReflowWidth, 640);
        try
        {
            string[] findings = await page.EvaluateAsync<string[]>(HorizontalScroll);
            Fail(findings, $"The page scrolls sideways at {ReflowWidth} CSS pixels (WCAG 2.2, 1.4.10)");
        }
        finally
        {
            if (before is not null)
            {
                await page.SetViewportSizeAsync(before.Width, before.Height);
            }
        }
    }

    // Observe the current document before, during and briefly after an optional interaction.
    public static async Task AssertNoMotionAsync(IPage page, Func<Task>? interact = null)
    {
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        await using IJSHandle observer = await page.EvaluateHandleAsync(Motion);
        try
        {
            if (interact is not null)
            {
                await interact();
            }

            string[] findings = await observer.EvaluateAsync<string[]>("watch => watch.finish()");
            Fail(findings, "Motion under reduced motion (WCAG 2.2, 2.3.3)");
        }
        finally
        {
            await observer.EvaluateAsync("watch => watch.stop()");
        }
    }

    private static void Fail(string[] findings, string title)
    {
        if (findings.Length > 0)
        {
            Assert.Fail(string.Join(Environment.NewLine, [title + ":", .. findings.Select(finding => "  " + finding)]));
        }
    }

    private const string Describe = """
        const describe = el => {
          let text = '';
          const label = (el.getAttribute('aria-label') || el.textContent || '').replace(/\s+/g, ' ').trim();
          if (label) text = ` "${label.slice(0, 30)}"`;
          const name = el.id ? `#${el.id}` : el.classList.length ? '.' + [...el.classList].slice(0, 3).join('.') : '';
          return el.localName + name + text;
        };
        """;

    private const string TargetSizes = "() => {" + Describe + """
        const SELECTOR = 'a[href], button, input:not([type=hidden]), select, textarea, summary, ' +
          '[role=button], [role=link], [role=checkbox], [role=radio], [role=switch], [role=tab], ' +
          '[role=menuitem], [role=option], [role=slider], [role=treeitem], [tabindex]:not([tabindex="-1"])';
        const MIN = 24;
        const hitRects = el => {
          const style = getComputedStyle(el);
          if (el.closest('[hidden], [inert]') || style.visibility !== 'visible' || style.pointerEvents === 'none') return [];
          return [...el.getClientRects()].filter(r => r.width > 0 && r.height > 0);
        };
        const findings = [];
        for (const el of document.querySelectorAll(SELECTOR)) {
          if (el.matches(':disabled') || el.closest('[inert]')) continue;
          const rects = hitRects(el);
          if (el.matches('input[type=checkbox], input[type=radio]')) {
            for (const label of el.labels) rects.push(...hitRects(label));
          }
          if (rects.length === 0 || rects.some(r => r.width >= MIN && r.height >= MIN)) continue;
          const sizes = rects.map(r => `${r.width.toFixed(1)} by ${r.height.toFixed(1)} px`);
          findings.push(`${describe(el)}: ${sizes.join('; ')}`);
        }
        return findings;
        }
        """;

    private const string HorizontalScroll = "() => {" + Describe + """
        const root = document.documentElement;
        if (root.scrollWidth <= root.clientWidth) return [];
        const scrollsInside = el => {
          for (let node = el.parentElement; node && node !== root; node = node.parentElement) {
            const overflow = getComputedStyle(node).overflowX;
            if (overflow !== 'visible' && overflow !== 'clip') return true;
          }
          return false;
        };
        const outside = new Set([...document.body.querySelectorAll('*')].filter(el => {
          const box = el.getBoundingClientRect();
          return box.width > 0 && (box.right > root.clientWidth + 0.5 || box.left < -0.5) && !scrollsInside(el);
        }));
        const outermost = [...outside].filter(el => !outside.has(el.parentElement));
        return [
          `The page is ${root.scrollWidth} px wide in a viewport of ${root.clientWidth} px. Reaching beyond it:`,
          ...outermost.slice(0, 5).map(el => `${describe(el)}, ${Math.round(el.getBoundingClientRect().width)} px wide`),
        ];
        }
        """;

    private const string Motion = "() => {" + Describe + """
        const findings = [];
        if (!matchMedia('(prefers-reduced-motion: reduce)').matches) {
          findings.push('The browser does not report reduced motion.');
        }
        if (getComputedStyle(document.documentElement).scrollBehavior === 'smooth') {
          findings.push('The page scrolls smoothly.');
        }
        const moved = new Set();
        let last = new Map();
        let frame;
        const sample = () => {
          const current = new Map();
          for (const el of document.body.querySelectorAll('*')) {
            const box = el.getBoundingClientRect();
            if (!box.width && !box.height) continue;
            const style = getComputedStyle(el);
            const geometry = [box.x, box.y, box.width, box.height, style.transform, style.rotate, style.scale, style.translate].join('|');
            const previous = last.get(el);
            const changed = previous && geometry !== previous.geometry;
            // An instant open or reflow jumps once; motion passes through intermediate frames.
            if (changed && previous.changed) moved.add(el);
            current.set(el, { geometry, changed });
          }
          last = current;
          frame = requestAnimationFrame(sample);
        };
        sample();
        return {
          stop: () => cancelAnimationFrame(frame),
          finish: async () => {
            await new Promise(resolve => setTimeout(resolve, 400));
            const outermost = [...moved].filter(el => !moved.has(el.parentElement));
            return findings.concat(outermost.slice(0, 5).map(el => `${describe(el)} changes geometry across frames`));
          }
        };
        }
        """;
}
