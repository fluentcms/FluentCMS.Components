# How a component is drawn and specified

The recipe of the design system. It is written from the three exemplars on [foundations.html](foundations.html): button, field and text input. Every other component follows it, so that the pages read as one system and the CSS can become the library's stylesheet as it is.

Read the exemplars first: `css/components/button.css`, `field.css` and `input.css`, and their sections on the page.

## 1. What is delivered for a component

- `css/components/<name>.css`, or `css/patterns/<name>.css` for a CSS-only pattern. One file per component.
- A section on the page of its lane: every variant, size and state, the modes strip, and the spec block.
- Nothing else. `tokens.css`, `base.css`, `docs/` and `icons/` are not edited by a lane.

## 2. Names

| Thing | Form | Example |
| --- | --- | --- |
| Block | `fl-<name>`, the component without `Fl`, in kebab case | `fl-button`, `fl-radio-group` |
| Part | `fl-<name>__<part>` | `fl-field__label`, `fl-input__action` |
| Modifier | `fl-<name>--<modifier>` | `fl-button--primary` |
| Size | `--s` and `--l`; medium has no class | `fl-input--s` |
| Token of a component | `--fl-<name>-<property>` | `--fl-button-height` |
| File | the block without `fl-` | `button.css` |

- The box of a text control is `fl-input`. The number, date and time inputs use the same box and add a file for their own parts.
- **State comes from the real attribute, never from a class**: `disabled`, `readonly`, `[aria-invalid="true"]`, `[aria-pressed="true"]`, `[aria-selected="true"]`, `[aria-current]`, `[aria-expanded]`, `[aria-busy="true"]`, `[open]`, `:checked`, `:indeterminate`, `:popover-open`. There are no `is-` classes.
- A modifier names what a thing is (`--primary`, `--horizontal`), not how it looks (`--blue`, `--big-padding`).
- A part is a direct child where it can be, and the selector says so: `& > .fl-input__control`.

## 3. The CSS

```css
/* Button: on a <button>, or on an <a> that goes somewhere. */

@layer fl.components {
  .fl-button {
    /* 1. Tokens of the component, from global tokens */
    --fl-button-height: var(--fl-control-height-m);
    --fl-button-bg: var(--fl-color-surface);

    /* 2. The rule of the block, reading its tokens */
    block-size: var(--fl-button-height);
    background: linear-gradient(var(--fl-button-overlay) 0 0) var(--fl-button-bg);

    /* 3. Parts */
    & > .fl-icon { color: var(--fl-button-icon); }

    /* 4. Variants and sizes: tokens only */
    &.fl-button--primary { --fl-button-bg: var(--fl-color-accent); }

    /* 5. States, once, in the fixed order */
    &:hover { --fl-button-overlay: var(--fl-button-bg-hover); }
    &:disabled { background-color: var(--fl-color-disabled-fill); }

    /* 6. Forced colours, reduced motion */
  }
}
```

- Everything is inside `@layer fl.components`, in one block per component, with native nesting.
- **Tokens only.** No raw colour, length, font family, font size, weight, duration or shadow. Allowed without a token: `0`, `1`, `100%`, `fr`, `auto`, numbers inside `calc()`, `1lh` and `em` for an offset that follows the text, `currentColor`, `transparent`, system colours inside `@media (forced-colors: active)`, and the length in a container or media query, since a query cannot read a token.
- **Logical properties only**: `inline-size`, `padding-inline`, `margin-block-start`, `inset-inline-end`, `border-start-start-radius`, `text-align: start`. Never `left`, `right`, `width`, `height`, `margin-top`.
- **Variants set tokens.** A variant or size sets the component's tokens and nothing else. This keeps every state rule at the same specificity.
- **States are written once, in this order**, so that a later one wins: hover, active, focus-visible, selected or pressed, read-only, invalid, disabled, busy.
- The block sets its own `font-family`, `font-size`, `line-height` and `color` from tokens. It does not count on the page for them.
- A component styles only its own block and parts. It may place another component inside it (`& > .fl-icon`), and never restyles one from outside.
- No `!important`, no id selectors, no element selectors outside the block, no `@import`.
- Breakpoints are container queries where a component reacts to its own space. The two widths in use are `30rem` and `48rem`.
- **Forced colours**: borders carry the shape, so a border that is not wanted is `transparent`, never `none`. Disabled is `GrayText`, selected is `Highlight`. Focus is always an outline, which the system keeps.
- **Reduced motion**: transitions use the duration tokens, which are zero under `prefers-reduced-motion`. A continuous animation stops there and shows a still form.
- No inline `style` in the markup of a component.

## 4. Which tokens a component may read

1. The global tokens of `css/tokens.css`.
2. Its own tokens, declared on its block.
3. Never the tokens of another component.

| Need | Token |
| --- | --- |
| Height of a control | `--fl-control-height-s`, `-m`, `-l` |
| Inline padding of a control | `--fl-control-padding-s`, `-m`, `-l` |
| Height of an item in a list (menu item, option, navigation link, tree item) | `--fl-item-height` |
| Height of a row of one line (table, list) | `--fl-row-height` |
| Padding of a region | `--fl-inset-s` (toolbar, cell), `-m` (card, popover), `-l` (page, dialog) |
| Gap between siblings | `--fl-gap-s`, `-m`, `-l` |
| A small fixed size (dot, mark, avatar, track) | the space scale, `--fl-space-*` |
| Width of a container | `--fl-size-*`, `--fl-measure` |
| Icon | `--fl-icon-size-s`, `-m`, `-l` |

The first six follow the density. A thing that should tighten in compact density reads them; a thing that should not reads the space scale.

**A missing token is not added by a lane.** Use the nearest one and list the request in the pull request under "Token requests", with the value wanted and where it is used.

## 5. Looks that every component shares

| Look | Rule |
| --- | --- |
| Radius | Controls `--fl-control-radius`; marks, keys, tags `--fl-radius-s`; cards, menus, popovers `--fl-radius-l`; dialogs `--fl-radius-xl`; pills `--fl-radius-full` |
| Text | Controls, navigation, tables `--fl-font-size-s`; body text and the value of an input `--fl-font-size-m`; help and meta `--fl-font-size-xs` |
| Border of a control that takes input | `--fl-color-border-control`; on hover `--fl-color-text-subtle` |
| Border of a button or a chip | `--fl-color-border-strong` |
| Divider, outline of a card | `--fl-color-border` |
| Hover and pressed on a neutral thing | `--fl-color-hover` and `--fl-color-pressed`, painted over its background |
| Selected or current | `--fl-color-accent-soft` background, `--fl-color-accent-text` text; a filled mark is `--fl-color-accent` with `--fl-color-on-accent` |
| Focus | `outline: var(--fl-focus-ring); outline-offset: var(--fl-focus-ring-offset); box-shadow: var(--fl-focus-halo)` on `:focus-visible`. A text control draws the outline on its border: `outline-offset: calc(-1 * var(--fl-border-width))`, no halo. An item inside a clipped list draws it inside: `outline-offset: calc(-1 * var(--fl-focus-ring-width))` |
| Disabled | Fill `--fl-color-disabled-fill`, border `--fl-color-disabled-border`, text and marks `--fl-color-disabled-text`, `cursor: not-allowed`, no hover |
| Read-only | Fill `--fl-color-surface-subtle`, border `--fl-color-border-strong`, text unchanged |
| Invalid | Border `--fl-color-danger-solid`; the message is in the field, in `--fl-color-danger-text`, with an icon |
| Placeholder | `--fl-color-text-subtle` |
| Icon in a control | `--fl-icon-size-m`, in `--fl-color-text-muted` or the colour of the text |
| What floats (menu, popover, toast) | `--fl-color-surface-raised`, `--fl-shadow-m`, `--fl-radius-l`, a border in `--fl-color-border` |
| Dialog, drawer | `--fl-color-surface-raised`, `--fl-shadow-l`, backdrop `--fl-color-backdrop` |
| Tone | `soft` background with `text` on it and `border` around it; `solid` for a mark or a strong fill with `on-solid` on it. A tone never carries a meaning alone |

## 6. States, and how a static page shows them

A component shows every state it has: rest, hover, focus, pressed, selected, disabled, read-only, invalid, busy or loading, empty.

| State | On the page |
| --- | --- |
| Hover | `data-force="hover"` on the element that would be pointed at |
| Focus | `data-force="focus-visible"` on the element that would have focus; `focus-within` likewise |
| Pressed | `data-force="hover active"` |
| Selected, expanded, current | the real attribute: `aria-pressed`, `aria-selected`, `aria-expanded`, `aria-current`, `checked`, `open` |
| Disabled, read-only | `disabled`, `readonly`; on a link `aria-disabled="true"` |
| Invalid | `aria-invalid="true"` |
| Busy | `aria-busy="true"` |
| Mixed checkbox | `data-indeterminate` on the input; the page script sets the property |
| Open overlay | drawn open in the page, in a frame, next to a live one that opens with its real trigger |

`data-force` works because `docs/docs.js` gives every rule that holds `:hover`, `:active`, `:focus-visible` or `:focus-within` a twin that matches the attribute. The component's CSS holds no rule for it. Put the attribute on the element that the pseudo-class would match, also when the rule reads it through `:has()`.

States go in a matrix: variants down, states across.

```html
<div class="docs-demo docs-scroll">
<table class="docs-matrix">
<thead><tr><td></td><th scope="col">Rest</th><th scope="col">Hover</th></tr></thead>
<tbody>
<tr><th scope="row">Primary</th><td>…</td><td>…</td></tr>
</tbody>
</table>
</div>
```

## 7. Dark, compact and right to left

- The switches in the header set the theme, the density and the direction of the whole page. Every section must hold in each of them. A page also takes them from its address: `?theme=dark&density=compact&dir=rtl`.
- Each component ends with a **modes strip**: a `<div class="docs-modes">` that holds one representative group of markup. The page script repeats it three times: in the other theme, the other density and the other direction. Add `docs-demo--column` to stack its content. Ids inside it are renamed in the copies.
- Dark needs no rule in a component: the tokens carry it. If a component looks wrong in dark, the fault is a raw value or a wrong token.
- Compact needs no rule either, when heights, paddings and gaps come from the tokens of section 4.
- Right to left needs logical properties, and `fl-icon--directional` on an icon that points along the reading direction (an arrow, a chevron that means "next"). Use `:dir(rtl)` for the rare rule that must differ. A number with a separator, such as a counter, keeps its order with `unicode-bidi: plaintext`.

## 8. The spec block

Each component ends with one table, after the modes strip.

```html
<table class="docs-spec">
<caption>Spec</caption>
<tbody>
<tr><th scope="row">Element</th><td>…</td></tr>
<tr><th scope="row">ARIA and keys</th><td>…</td></tr>
<tr><th scope="row">Level</th><td>…</td></tr>
<tr><th scope="row">Parameters</th><td>…</td></tr>
<tr><th scope="row">Classes</th><td>…</td></tr>
<tr><th scope="row">Tokens</th><td>…</td></tr>
</tbody>
</table>
```

| Row | Holds |
| --- | --- |
| Element | The HTML element the component is built on, and the element of each part that matters |
| ARIA and keys | The pattern of the ARIA Authoring Practices it follows, the attributes it sets, every key and what it does, and where focus goes |
| Level | Static, Native or Interactive, as section 4.4 of the plan defines them. For Interactive, what the static fallback is |
| Parameters | The names a Blazor component would expose, in Pascal case, with the values of each. Few: what a back-office screen needs |
| Classes | The block, its modifiers and its parts |
| Tokens | The global tokens it reads, grouped, and its own tokens |

A CSS-only pattern has no Parameters row.

## 9. The page

```html
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Forms · FluentCMS Components design system</title>
<link rel="icon" href="data:,">
<link rel="stylesheet" href="css/tokens.css">
<link rel="stylesheet" href="css/base.css">
<link rel="stylesheet" href="css/components/icon.css">
<!-- one <link> for each component and pattern the page shows -->
<link rel="stylesheet" href="docs/docs.css">
<script src="docs/docs.js"></script>
</head>
<body class="fl-root docs">
<div class="docs-layout">
<main class="docs-main">
<h1>Forms</h1>
<p class="docs-lede">…</p>

<section class="docs-section" id="select">
<h2>Select</h2>
<p>What it is for, in a sentence or two.</p>
<h3>Variants</h3>
<div class="docs-demo">…</div>
<h4>A caption under a demo, when it needs one</h4>
…
</section>

</main>
</div>
</body>
</html>
```

The header, the list of sections and the switches are built by the script. The classes of the chrome:

| Class | For |
| --- | --- |
| `docs-section` with an `id` | One component; its `<h2>` goes in the list of sections |
| `docs-demo` | A frame around library markup, laid out as a wrapping row. `--column` stacks, `--canvas` puts it on the page background, `--narrow` limits the width of its children |
| `docs-scroll` | Lets a wide thing scroll inside itself, so the page never scrolls sideways |
| `docs-matrix` | The table of states |
| `docs-modes` | The modes strip |
| `docs-spec`, `docs-table` | The spec block; any other table of the documentation |
| `docs-note` | A decision or an open question, such as O8 and O9 |
| `docs-block` | A placeholder for content inside a layout demo |
| `docs-narrow`, `docs-medium` | A width (11 and 16 rem) for a control that fills its container. The only docs classes that go on library markup |

- Words on a component page are generic: Save, Name, Email, Project. Words of a CMS (article, publish, locale) appear only in the reference screens.
- Every example is real markup with the real element: `<dialog>`, `popover`, `<details>`, native inputs, a `<label for>` on every control. A few lines of script in the page for a demo are fine.
- An icon is `<svg class="fl-icon" aria-hidden="true"><use href="icons/lucide.svg#name"/></svg>`. The `<svg>` carries no `viewBox`: the symbol has its own, and a second one shifts and clips the drawing. The names are on the tokens page. A missing icon is requested like a missing token.

## 10. Accessibility floor

- Every target is at least 24 by 24 CSS pixels, in both densities. `--fl-target-min` holds the number.
- The focus ring is visible on every background the component can sit on.
- No action depends on hover alone: what hover shows, focus shows too, and a touch can reach it.
- Colour is never the only sign: an error has an icon and a text, a selected thing has a mark or a weight, a link in text is underlined.
- Text has 4.5:1 on its background, and a border or mark that is needed to see a control has 3:1. `node design/tools/contrast.mjs` checks the token pairs; a pair it does not list is not to be used for text.
- Every control has a name: a `<label>`, or `aria-label` when there is no visible text.

## 11. Before the pull request

Serve `design/` and open the page by `http://localhost:<port>/system/<page>.html`; a browser tool cannot act on a `file://` page.

1. At 1440 by 900 and at 1280 by 800: no horizontal page scroll (`scrollWidth` equals `clientWidth`), no wrapped label, no clipped text.
2. At 320 wide: the same, with wide things scrolling inside `docs-scroll`.
3. In dark, in compact density and in right to left: each once, over the whole page.
4. No error in the console.
5. `node design/tools/contrast.mjs` passes.
6. `npx --yes markdownlint-cli2` passes for any Markdown touched.
