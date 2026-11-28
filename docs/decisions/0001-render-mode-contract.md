# 0001. Render mode contract

Status: accepted, 27 November 2026.

## Context

A Blazor application renders in one of three ways. In static server-side rendering (static SSR) the server sends HTML and no .NET code runs in the browser afterwards. In interactive server mode a circuit keeps the components alive on the server. In interactive WebAssembly mode they run in the browser. One page may mix them, and a component that is prerendered on the server may then become interactive.

The library is meant for all three (decision D5 of the [implementation plan](../planning/implementation-plan.md)). The FluentCMS admin runs interactively, but its hosted sign-in pages are static, and other applications will choose for themselves. A component that quietly needs one mode would fail in the others, usually at run time and usually in the mode nobody tested.

Components also differ in how much they can do with no code running. A button or a card is markup. A dialog can be built on the native `<dialog>` element and a small script. A table with sorting callbacks needs .NET events.

## Decision

1. **The library never sets a render mode.** No component carries `@rendermode`. The host decides, per page or per component, and every component works under that choice.
2. **Each component declares one of three levels**, stated on its gallery page and in its spec in the design system:
   - **Static.** Markup and CSS. It works fully with no code running.
   - **Native.** Its behaviour comes from native elements and the library's one script. It works fully in static SSR.
   - **Interactive.** It needs an interactive mode for its full function, and renders a documented static fallback when there is none.
3. **Rules for every component:**
   - no service that exists in one mode only, so `AddFluentCmsComponents()` registers the same things for all of them;
   - nothing in `OnInitialized` that needs the browser (JavaScript interop, the user's clock, `window`); work that needs the browser waits for `OnAfterRenderAsync`;
   - prerendering followed by interactivity does not flash ~~or lose focus~~, so the interactive render produces the markup the prerender produced (the [update below](#update-focus-across-prerendering) supersedes the focus promise);
   - no synchronous JavaScript interop, which exists in WebAssembly only.
4. **The browser tests run every gallery page in the three modes**, so a violation fails a pull request and not a user.

## Consequences

- A component is designed from its static form outward. The static form is the baseline and interactive behaviour is added to it, which keeps the HTML meaningful and usable from plain Razor Pages.
- Some components, such as the combobox and the tree, need a written static fallback and a test of it. That is more work per component, and it is the price of the promise.
- The gallery needs a host that can run the same page in each mode (issue C-05 of the plan).
- A host that wants one mode everywhere sets it once at its root and loses nothing.
- A feature that cannot meet these rules is not part of the library until it can. It is not allowed to work in one mode only.

## Update: focus across prerendering

As decided in [issue 41](https://github.com/fluentcms/FluentCMS.Components/issues/41), rule 3 still requires the transition to interactivity not to flash. Browser tests must compare the component's prerendered markup with its first interactive render: the markup must match, including IDs. Matching markup does not mean that the same DOM nodes survive.

Blazor replaces the prerendered contents of the interactive root, not static siblings elsewhere on the page. Focus and uncaptured edits inside that region can be lost. A host can capture an input's value before replacement, but the library restores neither focus nor edits across this transition. Returning focus to an overlay's opener when it closes is a separate rule in the [script policy](0003-script-policy.md#what-the-script-covers-because-a-target-browser-lacks-it).

A host that cannot accept this loss can [disable prerendering](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/prerender?view=aspnetcore-10.0#disable-prerendering) by setting `prerender: false` on the render mode of the top-level interactive boundary. For gallery pages that boundary is `Routes` in `App.razor`. A nested page cannot override its interactive parent's prerender setting.
