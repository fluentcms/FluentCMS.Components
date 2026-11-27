using Microsoft.Playwright;

namespace FluentCMS.Components.Browser.Harness;

// Checks of WCAG 2.2 that axe-core does not make, on a page that is already open.
public static class Wcag
{
    private const int ReflowWidth = 320;

    // Success criterion 2.5.8, target size (minimum): every target is at least 24 by 24 CSS pixels.
    // The criterion has exceptions, and two are applied here:
    //   - spacing: a target under 24 pixels passes when a circle of 24 pixels across, centred on it,
    //     meets no other target and no such circle of another small target;
    //   - inline: a link in a sentence or block of text.
    // The others (an equivalent control on the same page, a size set by the browser, an essential
    // size) cannot be read from the page. A check box or radio button counts together with its
    // label, since the label is what a pointer can hit; a disabled control is no target.
    public static async Task AssertTargetSizesAsync(IPage page)
    {
        string[] findings = await page.EvaluateAsync<string[]>(TargetSizes);
        Fail(findings, "Targets under 24 by 24 CSS pixels (WCAG 2.2, 2.5.8)");
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

    // Success criterion 2.3.3, animation from interactions, and the browser's own reduced-motion
    // setting: with the setting on, no animation or transition runs for more than a blink, the
    // page does not scroll smoothly, and nothing on it changes place while it sits.
    public static async Task AssertNoMotionAsync(IPage page)
    {
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        string[] findings = await page.EvaluateAsync<string[]>(Motion);
        Fail(findings, "Motion under reduced motion (WCAG 2.2, 2.3.3)");
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
          '[role=menuitem], [role=option], [role=slider], [tabindex]:not([tabindex="-1"])';
        const MIN = 24;
        const targets = [];
        for (const el of document.querySelectorAll(SELECTOR)) {
          if (el.disabled || el.closest('[inert], [hidden]')) continue;
          const style = getComputedStyle(el);
          if (style.display === 'none' || style.visibility === 'hidden') continue;
          const box = el.getBoundingClientRect();
          let rect = { left: box.left, top: box.top, right: box.right, bottom: box.bottom };
          if (el.matches('input[type=checkbox], input[type=radio]')) {
            for (const label of el.labels) {
              const l = label.getBoundingClientRect();
              rect = {
                left: Math.min(rect.left, l.left), top: Math.min(rect.top, l.top),
                right: Math.max(rect.right, l.right), bottom: Math.max(rect.bottom, l.bottom),
              };
            }
          }
          const width = rect.right - rect.left;
          const height = rect.bottom - rect.top;
          // Not rendered, or hidden by clipping to a pixel, as a skip link is until it has focus.
          if (width <= 1 && height <= 1) continue;
          targets.push({ el, rect, width, height, small: width < MIN - 0.01 || height < MIN - 0.01 });
        }

        const hasLetters = text => /[\p{L}\p{N}]/u.test(text);
        const inText = el => {
          for (let node = el.parentElement; node; node = node.parentElement) {
            const text = [...node.childNodes].filter(n => n.nodeType === Node.TEXT_NODE).map(n => n.textContent).join('');
            if (hasLetters(text)) return true;
            if (getComputedStyle(node).display !== 'inline') return false;
          }
          return false;
        };

        const centre = t => ({ x: (t.rect.left + t.rect.right) / 2, y: (t.rect.top + t.rect.bottom) / 2 });
        const gapToRect = (c, r) => Math.hypot(Math.max(r.left - c.x, 0, c.x - r.right), Math.max(r.top - c.y, 0, c.y - r.bottom));
        const spaced = target => {
          const c = centre(target);
          return targets.every(other => {
            if (other === target || other.el.contains(target.el) || target.el.contains(other.el)) return true;
            return other.small
              ? Math.hypot(c.x - centre(other).x, c.y - centre(other).y) >= MIN - 0.01
              : gapToRect(c, other.rect) >= MIN / 2 - 0.01;
          });
        };

        return targets
          .filter(t => t.small)
          .filter(t => !(getComputedStyle(t.el).display === 'inline' && inText(t.el)))
          .filter(t => !spaced(t))
          .map(t => `${describe(t.el)} is ${t.width.toFixed(1)} by ${t.height.toFixed(1)} px`);
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

    private const string Motion = "async () => {" + Describe + """
        const findings = [];
        if (!matchMedia('(prefers-reduced-motion: reduce)').matches) {
          findings.push('The browser does not report reduced motion.');
        }
        if (getComputedStyle(document.documentElement).scrollBehavior === 'smooth') {
          findings.push('The page scrolls smoothly.');
        }
        for (const animation of document.getAnimations()) {
          const end = animation.effect?.getComputedTiming().endTime ?? 0;
          if (animation.playState === 'running' && end > 100) {
            findings.push(`${describe(animation.effect.target)} runs ${animation.animationName ?? animation.transitionProperty}`);
          }
        }

        const frame = () => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
        const snapshot = () => new Map([...document.body.querySelectorAll('*')].map(el => [el, el.getBoundingClientRect()]));
        await frame();
        const first = snapshot();
        await new Promise(resolve => setTimeout(resolve, 400));
        await frame();
        const moved = [...snapshot()].filter(([el, box]) => {
          const before = first.get(el);
          return before && (Math.abs(before.left - box.left) > 0.5 || Math.abs(before.top - box.top) > 0.5);
        }).map(([el]) => el);
        const outermost = moved.filter(el => !moved.includes(el.parentElement));
        findings.push(...outermost.slice(0, 5).map(el => `${describe(el)} changes place while the page sits`));
        return findings;
        }
        """;
}
