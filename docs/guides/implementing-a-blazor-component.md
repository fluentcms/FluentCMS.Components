# Implementing a Blazor component

Use `FlButton` as the worked example for turning a design-system specimen into a component. Start with the [visual and CSS recipe](../../design/system/RECIPE.md) and the [reference markup](../../design/system/foundations.html#button). This guide covers the Blazor work: the HTML contract, the public API, the gallery and the tests. The [eight gates in the plan](../planning/implementation-plan.md#11-definition-of-done-for-a-component) are the acceptance criteria.

## Start with the HTML

Ask what the control does before adding a callback. `FlButton` has two native forms: a `button` for an action and an `a` with `href` for a destination. Its default is `type="button"` so putting it in a form does not accidentally submit the form. When a host requests `Submit`, the browser can submit without Blazor interactivity or JavaScript:

```razor
@using FluentCMS.Components

<form method="get" action="search">
    <label for="query">Search terms</label>
    <input id="query" name="q" />
    <FlButton Type="FlButtonType.Submit" Icon="FlIcons.Search">Search</FlButton>
</form>
```

Here `search` is the host's destination. `OnClick`, by contrast, requires an interactive parent. That distinction is why the gallery declares `FlButton` **Static**, not why the component should set `@rendermode`. The host chooses the mode; see [ADR 0001](../decisions/0001-render-mode-contract.md).

## Put the contract in the component

- [FlButton.razor](../../src/FluentCMS.Components/Foundations/FlButton.razor) renders the native element and the content shared by both forms. Its explicit `@namespace FluentCMS.Components` keeps the public name independent of the folder. The sealed [partial class](../../src/FluentCMS.Components/Foundations/FlButton.razor.cs) holds parameters, validation, attribute filtering and class selection. There is no common base class to subclass.
- Keep component-specific enums beside the component; reuse a common type such as `FlSize` when the values mean the same thing. Map options to the **existing** `fl-` classes. Secondary and Medium need no modifier. Do not fork the library stylesheet in the Razor component.
- Use a parameter for a choice the component owns. For everything else, capture unmatched attributes, append the host's `class`, and put the remaining attributes on the native element. Do not mutate the host's dictionary. Compare owned attribute names without regard to case: a host-supplied `TYPE` or `href` must not undo the component's action policy. Preserve unrelated attributes and focus/blur observers.
- Validate contradictory inputs when parameters change. A text button requires `ChildContent`; the component cannot inspect an arbitrary fragment to prove it contains meaningful words. An icon-only button requires one leading icon and a nonblank `Label`, with no visible child content or trailing icon. A link cannot also request toggle or form-button semantics.
- Register the resulting public signatures, including nullability, in [PublicAPI.Unshipped.txt](../../src/FluentCMS.Components/PublicAPI.Unshipped.txt). Let a build catch missing entries before writing a gallery example against an accidental API.

### Keep state and action separate

| State | What `FlButton` renders or does |
| --- | --- |
| Toggle | `Pressed=null` omits `aria-pressed`. `true` and `false` render that exact state. The caller changes it; clicking does not silently invert a parameter. |
| Disabled | A native button gets `disabled`. An unavailable link loses `href` and keeps `role="link"`; a disabled link is not a Tab stop. Explicit `Disabled` takes precedence over `Busy`. |
| Busy | Keep the action's name, width and focus. Overlay a decorative spinner, set `aria-busy` and `aria-disabled`, and omit the activation callback. A submit/reset temporarily becomes `type="button"`; a busy link has no `href` but stays tabbable. |

CSS `pointer-events: none` cannot stop a keyboard submission. Test the native default action, not just the callback count. Conversely, a focusable native button may still emit a DOM event: the contract is that the **component** does not invoke its action, submit, reset or navigate, not that it can silence arbitrary listeners in the host.

Use the [internal text lookup](../../src/FluentCMS.Components/FlTextExtensions.cs) for words owned by the library. While busy, the button describes itself with `FlText.Loading` rather than replacing the accessible name “Save” with “Loading”. A host can translate it; without a provider, or if the provider returns `null`, the helper supplies English. The action label is still the caller's text.

## Build the gallery page with the component

Follow the [gallery page conventions](../../CONTRIBUTING.md#writing-a-page). [Button.razor](../../gallery/Gallery.Client/Pages/Button.razor) renders actual `FlButton` instances, registers a route and demonstrates where static functionality ends. Add its entry in [GalleryPages.cs](../../gallery/Gallery.Client/GalleryPages.cs). Give interactive examples an observable result; for example, the button page exposes a click count, a controlled toggle, and a real GET form with a named submitter.

Pass `Pressed`, `Disabled` and `Busy` to the specimens; `FlButton` renders the corresponding HTML and ARIA attributes. Setting `aria-pressed` or `aria-busy` as an unmatched attribute is not a substitute. Only the gallery assigns `data-force` to show states such as hover in its matrix. Its [external script](../../gallery/Gallery.Client/wwwroot/button-states.js) adds matching CSS rules scoped to that table. The component neither creates nor interprets those forced states, and the script must not appear in the NuGet package.

## Prove the eight gates

| Gate | Evidence to collect, following the button |
| --- | --- |
| 1. Design match | Compare the actual gallery controls with the reference, not the surrounding page. Preserve intentional empty matrix cells. Record which sizes and states were compared and any differences. |
| 2. Tokens and appearance | Use the library CSS; check light, dark, compact, RTL and forced colours. Compare control geometry as well as paint. |
| 3. Render modes | Exercise the declared level in static SSR, interactive server and WASM. [ButtonPrerenderTests](../../tests/FluentCMS.Components.Browser/Foundations/ButtonPrerenderTests.cs) hold startup on one navigation, capture the prerendered controls, prove the nodes were replaced, and compare the first interactive markup, including IDs and nested icons. |
| 4. Keyboard | Use real Tab, Enter and Space. [ButtonTests](../../tests/FluentCMS.Components.Browser/Foundations/ButtonTests.cs) include a JavaScript-disabled form and a working submitter as the positive control for the busy one. Verify focus as well as the result. |
| 5. Accessibility and layout | Run axe in the requested appearances, measure actual targets in **both** densities, check 320px reflow and exercise reduced motion during an interaction. See [ButtonAccessibilityTests](../../tests/FluentCMS.Components.Browser/Foundations/ButtonAccessibilityTests.cs). |
| 6. Parameters, ARIA and text | [FlButtonTests](../../tests/FluentCMS.Components.Tests/Foundations/FlButtonTests.cs) render each form, exercise callbacks, rerender when state changes, and check localized/fallback text. Include a case that would fail if a forbidden action were restored. |
| 7. Gallery | Show all variants, sizes and applicable states, the parameter and spec tables, and an example a host can copy. Check direct navigation and the interactive takeover; a static screenshot cannot establish behavior. |
| 8. Package | Build with warnings as errors, check the public API and pack Release. Inspect the `.nupkg` and its dependency group, read the compressed-size report, and verify gallery-only assets are absent. An isolated consumer should be able to render the packed component. |

With the [browser prerequisites](../../CONTRIBUTING.md#browser-tests) installed and all three render modes enabled, these commands run the button-specific checks:

```powershell
dotnet test --project tests/FluentCMS.Components.Tests --filter-class '*FlButtonTests'
dotnet test --project tests/FluentCMS.Components.Browser --filter-class 'FluentCMS.Components.Browser.Foundations.Button*'
```

Run the full PR checks before merge. Record the engines, failures, intentional skips, and separate published-site checks; a WebKit run is not a Safari run. The [FlButton PR](https://github.com/fluentcms/FluentCMS.Components/pull/62) shows the evidence for this exemplar.
