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
   - prerendering followed by interactivity does not flash or lose focus, so the interactive render produces the markup the prerender produced;
   - no synchronous JavaScript interop, which exists in WebAssembly only.
4. **The browser tests run every gallery page in the three modes**, so a violation fails a pull request and not a user.

## Consequences

- A component is designed from its static form outward. The static form is the baseline and interactive behaviour is added to it, which keeps the HTML meaningful and usable from plain Razor Pages.
- Some components, such as the combobox and the tree, need a written static fallback and a test of it. That is more work per component, and it is the price of the promise.
- The gallery needs a host that can run the same page in each mode (issue C-05 of the plan).
- A host that wants one mode everywhere sets it once at its root and loses nothing.
- A feature that cannot meet these rules is not part of the library until it can. It is not allowed to work in one mode only.

## Update: focus across prerendering

Rule 3 said that "prerendering followed by interactivity does not flash or lose focus". The first half holds. The second cannot be kept, and this update replaces it.

- **What Blazor does.** When a prerendered page becomes interactive, Blazor replaces the prerendered DOM with the interactive one. An element that held focus loses it, and text typed into an input before that moment is gone.
- **Why the library does not repair it.** A script could bring focus back to the element, but not the text that was typed, and a repair of focus alone would hide half of the loss. The [script policy](0003-script-policy.md) lists no such behaviour, and none is added.
- **The rule that stays.** Nothing flashes: the interactive render produces the markup the prerender produced, ids included. That is tested, in the same browser tests as the rest of rule 3. Focus and typed text are lost by the framework's swap, and the library does not restore them.
- **What a host can do.** A host that cannot accept the loss turns prerendering off for that page or component.
