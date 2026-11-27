# 0002. Styling architecture

Status: accepted, 27 November 2026.

## Context

The library needs one look that a host can adjust: the accent, the radius, the density, the typeface and the theme. It has to sit beside whatever the host already uses, work under a strict content security policy, mirror for right-to-left languages, and be cheap to read and to change.

Section 4.2 of the [implementation plan](../planning/implementation-plan.md) proposed plain CSS before any style existed. The design system under `design/system/` has since been drawn, and it fixed the details: [RECIPE.md](../../design/system/RECIPE.md) says how a component is written, and [tokens.css](../../design/system/css/tokens.css) holds the tokens. This record states what they settled, so that the library's stylesheet follows them.

The alternatives were weighed in the plan. A CSS framework such as Bootstrap or Tailwind would clash with a host's own copy, its resets would restyle the host's pages, its utility classes would end the use of the same classes in plain HTML, and it would bring a Node toolchain into a .NET repository. A preprocessor such as Sass adds a build step, and its variables cannot change at run time, which themes need. Blazor's CSS isolation ties styles to components with scope attributes, so plain HTML could no longer look the same.

## Decision

1. **Plain CSS.** No framework, no preprocessor and no build tool. Native nesting, custom properties, cascade layers, `:has()`, container queries, `light-dark()` and `color-mix()` are in every target browser. The stylesheet is one file per component, joined into `fluentcms-components.css` by MSBuild. A minifier or compiler is added only if the size check or real repetition asks for it. Plain CSS is valid SCSS, so the choice is cheap to reverse.
2. **Three cascade layers**, declared once in this order: `fl.base`, `fl.tokens`, `fl.components`. They keep the library's rules below a host's own unlayered styles, so a host overrides without raising specificity. The one `!important` is the rule in `fl.base` that lets the `hidden` attribute hide an `fl-` element whatever display a component sets.
3. **Tokens in two tiers.** Global tokens (`--fl-color-accent`, `--fl-space-3`, `--fl-radius-m`) are declared in `fl.tokens`. A component may add its own, declared on its block (`--fl-button-height`) and computed from global ones. A component reads global tokens and its own, never another component's. No raw colour, length, font size, weight, duration or shadow is written in a component rule.
4. **Themes through `light-dark()`.** A colour token holds both themes in one value, so no token is declared twice. The theme is the `color-scheme` of an element: `data-fl-theme="light"` or `"dark"` on any ancestor sets it, and where the attribute is absent the page follows `prefers-color-scheme`. A component needs no dark rule; one that looks wrong in dark has a raw value or a wrong token.
5. **One accent token and one radius token.** A host sets `--fl-color-accent` (and `--fl-color-on-accent` when white is not readable on it), and the hover, pressed, soft, border and text variants are computed from it with `color-mix()`. A host sets `--fl-radius-m`, and the small, large and extra-large steps are computed from it. `data-fl-accent` on an element sets an accent below the root.
6. **Density through `data-fl-density`** (`comfortable` or `compact`) on any ancestor. Heights, paddings, gaps and row sizes read tokens that change with it. Type sizes do not change.
7. **Logical properties only** (`margin-inline-start`, `inline-size`), so `dir="rtl"` mirrors the layout with no second stylesheet. An icon that points along the reading direction carries `fl-icon--directional`.
8. **State comes from the real attribute**, never from a class: `disabled`, `aria-invalid`, `aria-pressed`, `aria-selected`, `aria-current`, `aria-expanded`, `[open]`, `:checked`, `:popover-open`. Where no attribute exists, a `data-fl-` attribute is used, such as `data-fl-active`.
9. **Names.** Classes are `fl-<block>`, `fl-<block>__<part>` and `fl-<block>--<modifier>`, custom properties are `--fl-…` and data attributes are `data-fl-…`. A modifier names what a thing is, not how it looks.
10. **No inline script and no injected `<style>` element**, so the library runs under a content security policy that allows scripts from the site alone. Avoiding inline `style` attributes as well, so that a host can refuse those too, is a goal tracked per component.
11. **One file per component** under `css/components/`, and one per CSS-only pattern under `css/patterns/`. The token file and the component files of the design system become the library's as they are. The design system pages and the gallery load the same stylesheet the package ships.

## Consequences

- A host re-skins the library by setting a few custom properties, and cannot do more. That is intended: the library has one look (section 3 of the plan).
- Component rules are short and regular, which makes review mechanical. The recipe's rules (no raw values, no physical properties, no id selectors) can be checked by a script later.
- Because every library rule is in a layer, a host's unlayered CSS always wins, which is what a host expects. A host that layers its own CSS must declare its layers after `fl`.
- The stylesheet uses features that exist only in the last two versions of Chrome, Edge, Firefox and Safari, and has no fallback for older browsers.
- Plain HTML and Razor Pages can use the same classes without Blazor. The cost is that the markup of a component is public: renaming a class or a part is a breaking change.
- If a future need cannot be met in plain CSS, the answer is a new record that adds a build step, not an exception inside one component.
