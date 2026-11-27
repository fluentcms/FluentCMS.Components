# 0003. Script policy

Status: accepted, 27 November 2026.

## Context

Section 4.3 of the [implementation plan](../planning/implementation-plan.md) proposes one script for the library, which adds only what HTML lacks, and a spike that sets which native features the script has to cover. The design system under `design/system/` was drawn before that spike. It leans on recent platform features (anchor positioning, invoker commands, interest invokers, `closedby`, `::details-content`, `light-dark()`), and it had only been seen in Chromium. Its page scripts stand in for the library's script, and some of their fallbacks were written without a browser that needed them.

This record holds the result of the spike (issue 28) and the policy that follows from it. The target browsers are the last two versions of Chrome, Edge, Firefox and Safari.

### What was run

Every probe ran against the design system pages, served over HTTP from `design/`, on Windows 11.

| Engine | Build | What it is |
| --- | --- | --- |
| Chromium | Chrome 158.0.8127.38 | The Chrome installed on the test machine. |
| Chromium | Edge 158.0.4370.45 | The Edge installed on the test machine. |
| Chromium | Chrome for Testing 159.0.8154.6 | The build that Playwright 1.65 brings. Chrome 159 was the stable version on the day of the test. |
| Chromium | Chromium 156.0.8078.4 and 153.0.8010.12 | The builds of Playwright 1.64 and 1.63, older than the target, to see since when a feature holds. |
| Firefox | Firefox 161.0 and 160.0.1 | The release builds of Mozilla, the two newest versions on the day of the test, driven over WebDriver BiDi. |
| Firefox | Firefox 159.0, 157.0 and 155.0 | The builds of Playwright 1.65, 1.64 and 1.63, older than the target. |
| WebKit | WebKit builds that report Safari 27.2 (two revisions) and Safari 26.6 | The WebKit builds for Windows of Playwright 1.65, 1.64 and 1.63. |

Four kinds of probe were used:

1. **Detection**: `CSS.supports()` and a property check on a prototype, for every feature, in every build.
2. **Interaction**: a real pointer and a real keyboard on the live examples of `actions-overlays.html`, `forms.html`, `layout-navigation.html` and the settings screen. Each overlay was opened, moved through, closed with Esc, closed with a click outside, and the element with focus was read after each step. Places and sizes were measured, also with the trigger at the bottom of the view and after scrolling.
3. **The same interaction with the page script removed**, to see what the browser does alone.
4. **A sweep of every page and reference screen** at 1280 by 800: console errors, sideways scroll, and the size of every element with an `fl-` class, compared with Chrome. The 69 colour tokens were painted on a canvas in both themes and compared as well.

### What could not be checked

- **Real Safari.** It needs a Mac, and the test machine runs Windows. The WebKit builds above share the engine with Safari but not the platform layer of Apple. Two differences were seen and belong to the build, not to Safari as documented: it has no `date`, `time` or `datetime-local` input (the type falls back to `text`), and it does not know `overscroll-behavior`. Everything in the WebKit column below is therefore "seen in a WebKit build", never "seen in Safari".
- **Safari on iOS and iPadOS, Chrome and Firefox on Android, and any touch input.** The tooltip, the menus and the native date and time pickers behave differently under touch.
- **Screen readers.** Focus was read from the page; what NVDA or VoiceOver announce was not heard.
- **The pickers of the native date and time inputs.** They are drawn by the browser outside the page, and the browsers ran without a window.
- **Entry transitions in Firefox 160 and 161.** The release builds ran with reduced motion, where the duration tokens are zero. The transitions were seen in the Firefox 159 and 157 builds.
- **Forced colours and print.** They are not part of this spike.

## What was seen

### Feature by browser

"Yes" means that the feature was seen working on the design system page that uses it, in every build of the column heading. The older builds are named only where they differ.

| Feature | Used by | Chromium 158 and 159 | Firefox 160 and 161 | WebKit 26.6 and 27.2 |
| --- | --- | --- | --- | --- |
| `<dialog>` with `showModal()`: modal, the page behind it inert, Esc closes | Dialog, drawer, command palette, drawer of the narrow app shell | Yes | Yes | Yes |
| Focus on open goes to the control with `autofocus` | The same | Yes | Yes | Yes |
| Focus goes back to the opener on close | The same, and menu and popover | Yes | Yes | Only after a keyboard opening. After a pointer opening, focus falls to the `<body>` (see below). |
| `closedby="any"`: a click on the backdrop closes; a dialog without it stays | Dialog, drawer, command palette, drawer of the app shell | Yes | Yes | Yes |
| Invoker commands: `command="show-modal"` and `command="close"` with `commandfor` | The same | Yes | Yes | Yes |
| `popover` (auto) with `popovertarget` and `popovertargetaction="hide"`: light dismiss, Esc | Menu, popover, split button | Yes | Yes | Yes |
| `popover="manual"` | Toast region, list of the combobox | Yes | Yes | Yes |
| `popover="hint"`: a hint that opens leaves an open menu open | Tooltip | Yes | Yes | Yes |
| Interest invokers: `interestfor` | Tooltip | Yes | **No** | **No** |
| Anchor positioning with the implicit anchor (from `popovertarget`, from `showPopover({ source })`, from `interestfor`), `position-area` and `position-try-fallbacks` | Menu, popover, tooltip | Yes | Yes | Yes |
| `anchor-name`, `position-anchor`, `anchor()`, `anchor-size()` and `anchor-scope` | Combobox | Yes | Yes | Yes |
| `::details-content`, as a grid item and as `display: contents` | Disclosure, with and without actions | Yes | Yes | Yes |
| `<details name>`: one open at a time | Disclosure | Yes | Yes | Yes |
| Scroll-driven animation: `animation-timeline: scroll()` | Fade of the tabs | Yes | Yes. Firefox 159 has it; 157 and 155 do not. | Yes |
| `light-dark()`, `color-scheme` and `color-mix(in oklab, …)` | Tokens | Yes | Yes. All 69 colour tokens paint as in Chrome, in both themes. | Yes. The same. |
| `@layer`, native nesting, `:has()`, container queries | Every stylesheet | Yes | Yes | Yes |
| `1lh` | Alert, checkbox, field, menu, radio group, switch, text area, toast, timeline | Yes | Yes | Yes |
| `:dir()` | Icon, disclosure, drawer, navigation, number input, progress, tabs, tree | Yes | Yes | Yes |
| `field-sizing: content` | Text area that grows | Yes | Yes | Yes |
| `@starting-style` and entry transitions | Dialog, drawer, menu, popover, tooltip, toast, command palette | Yes | Yes, in the 159 and 157 builds (see "What could not be checked") | Yes |
| Exit transitions: `overlay`, and `display` with `allow-discrete` | The same | Yes | **No** | **No** |
| First baseline of a `<textarea>` in a grid | Horizontal field | Yes | Yes | Yes |
| A floated `<legend>` as a grid item of its `<fieldset>` | Field around a group of controls | Yes | Yes | **No**: the legend keeps its place above the grid (see below). |
| Native `date`, `time` and `datetime-local` inputs in the box of the text input | Date input, time input | Yes, with the icon of the library on the button | Yes, with the buttons that Firefox draws | Not seen: the build has no such inputs |
| `overscroll-behavior: contain` | Lists inside overlays | Yes | Yes | Not in the build; Safari is documented to have it |
| `text-wrap: balance`, `100dvh`, `100vi` and `100vb`, `scrollbar-width`, `content-visibility`, `mask` without a prefix, `resize: block`, `rotate` and `translate` as properties | Several | Yes | Yes | Yes. `resize: block` is reported as known, but the build draws no handle. |

Details that the table cannot hold:

- **Placement.** A menu opens 4 px under its button and in line with its start edge, a menu with `--end` in line with its end edge, a tooltip centred over its control or on the side that its modifier names, and the list of the combobox under its box and as wide as it. With the trigger at the bottom of the view the menu and the list of the combobox flip to the other side, and both follow their trigger when the page scrolls. The numbers were the same in every build, within 2 px.
- **Baseline.** The baseline of the label lies 16 px under the top of its control for a text input and for a text area alike, in Firefox and in WebKit; in Chromium the two figures are 16.7 and 16.3 px.
- **Text area.** A text area with `fl-textarea--auto` grew from 73 to 174 px for eight lines, in every build, and did not scroll inside.
- **Date and time inputs in Firefox.** The box, the type, the three sizes and the states hold. Firefox draws its own calendar button and its own clock button, in its own colour; `::-webkit-calendar-picker-indicator` does not reach them.
- **Layout.** The sweep found no sideways scroll and no console error on any page, in any build. In Firefox 160 and 161 no element differs from Chrome in height, apart from text that wraps one line earlier or later; widths differ by 2 to 6 px where a label is set in the medium weight, which Firefox draws as regular on Windows while Chrome and WebKit draw it as semibold. In both WebKit builds one thing differs: the field on a `<fieldset>`.
- **The legend of a field in WebKit.** `field.css` floats the `<legend>` so that it lays out as a grid item of the `<fieldset>`. Chromium and Firefox do that. WebKit drops the float of a grid item, so the legend stays the legend of the fieldset and sits above the grid. A stacked field loses the 6 px between its label and its control. A horizontal field shows its label on a line of its own, and the control starts one line lower, level with the help text and not with the label.

### Without the feature

A feature has a row here when a build of a target browser lacks it. The middle column is what the reader gets with the page script removed.

| Feature | Missing in | What the reader gets without script | Verdict |
| --- | --- | --- | --- |
| `interestfor` | Firefox, WebKit | No tooltip appears, on hover or on focus. The control keeps its name, since `aria-label` carries it, but a shortcut that only the tooltip shows is lost. With the stand-in script of the page the tooltip opens on hover and on keyboard focus, closes on leave, blur, click and Esc, and sits where Chromium puts it, because `showPopover({ source })` sets the implicit anchor in every engine. | Degraded but usable. The script covers it. |
| Focus back to the opener after a pointer opening | WebKit | A click does not give focus to a button in WebKit, so the browser has no opener to return to, and focus lands on `<body>` when a dialog, a drawer, a menu or a popover closes. The next Tab goes on from the place of the overlay in the document, not from the opener: for a menu or a popover that follows its button that is the next control, and for a dialog it is wherever its markup lies. After a keyboard opening focus returns as elsewhere. | Degraded a little: nothing is lost, but the rule of the plan, focus back on the opener, does not hold. The script covers it. |
| Exit transitions | Firefox, WebKit | An overlay that closes disappears at once. Opening still fades and rises. Nothing is left on the screen and nothing jumps. | A working fallback. No script. |
| Scroll-driven animation | Firefox 157 and older, which are outside the target | The list of tabs has no fade at the end that hides tabs. It still scrolls, and a tab that takes focus scrolls into view. | Degraded but usable. No script. |
| The button of a native date or time input | Firefox | Firefox shows its own button in place of the icon of the library. | A working fallback. No script. |
| A floated `<legend>` as a grid item | WebKit | The label of a field on a `<fieldset>` sits above the grid, as described above. Every control is in its place and has its name. | Degraded in look only. No script: the markup or the rule of the field changes in a later issue. |
| Native date and time inputs | The WebKit build for Windows only | A text box that shows the value as `2026-11-24`. It says nothing about Safari. | Not a verdict on a target browser. |

No build of a target browser lacked anchor positioning, invoker commands, `closedby`, `popover="hint"`, `::details-content`, `<details name>` or `field-sizing`. The fallbacks that the design system wrote for them were therefore never seen at work: the `@supports (position-area: …)` branch in the menu, the popover and the tooltip, which lets the browser centre the popover in the view, and the click handlers for `commandfor` and `closedby` in the page scripts.

### With no script at all

With the page scripts removed, in every engine: a dialog and a drawer open from their button, hold focus, close with Esc, with a click on the backdrop and with their close button, and give focus back, in WebKit after a keyboard opening only; a menu and a popover open, sit at their button, and close with Esc and with a click outside; a disclosure opens and closes, and a named group keeps one open. What is lost is what HTML does not have: the arrow keys and the first focus of a menu, the state of `aria-expanded`, the shortcut of the command palette, the toasts, and, by their markup, the tabs, the tree and the combobox.

### One fault that every engine shares

A modal dialog makes everything outside it inert, and that includes a popover in the top layer. The toast region is such a popover. It is drawn over the backdrop of a dialog, as intended, but a toast in it cannot be pressed while the dialog is open: a click on its Undo button went to the dialog, and closed it where the dialog has `closedby="any"`. This is the behaviour of the platform, seen alike in Chromium, Firefox and WebKit. It is listed under the consequences, since the script places the region.

## Decision

### The policy

1. **One script.** It is a JavaScript initializer of the package (`FluentCMS.Components.lib.module.js`), which Blazor loads in every render mode with no setup by the host. It enhances the HTML that is already on the page, found by `data-fl-*` attributes and by the native attributes below, and it works again after an enhanced navigation. Where it can, it listens on the document and finds its component from the target of the event, so that markup added later needs no setup of its own.
2. **The native attribute is the contract.** A component renders `command` and `commandfor`, `popovertarget`, `popover`, `closedby`, `interestfor`, `autofocus` and `name` on a `<details>`, and the browser acts on them. The script never does again what every target browser does.
3. **It adds only what HTML lacks.** That is two short lists: what a target browser lacks today, and what no browser offers. Both are below, each item tied to its component.
4. **A fallback is tested on the feature, never on the name of the browser.** The check is a property on a prototype (`'interestForElement' in HTMLButtonElement.prototype`) or `CSS.supports()`. A fallback enters the script only for a feature that was seen missing in a target browser, and it leaves when the last two versions of every target browser have the feature.
5. **No call from .NET per interaction.** In the interactive modes .NET state is the source of truth, and the script reports a change through a DOM event that the component listens to. There is no synchronous interop, which exists in WebAssembly only.
6. **No inline script and no injected style**, so that the library runs under a content security policy that allows scripts from the site alone.

### What the script does not cover

These are native in every build that was run, and the script holds no code for them:

- Opening and closing a dialog or a drawer from a button, modality, keeping focus inside, Esc, and the click on the backdrop.
- Opening and closing a menu or a popover from its button, light dismiss and Esc.
- The place of a menu, a popover, a tooltip and the list of a combobox, and their flip at the edge of the view.
- Showing and hiding the content of a disclosure, and one open at a time in a named group.
- The theme, the growth of a text area, and the fade of the tabs.

Two items of section 4.3 of the plan leave the script for this reason: "placement of a popover where the browser has no anchor positioning" and "the dismissal rules of overlays". The click handlers for `commandfor` and `closedby` in the page scripts of the design system are not carried into the library.

### What the script covers because a target browser lacks it

| Component | What the script adds | Until |
| --- | --- | --- |
| `FlTooltip` | Where `interestfor` is missing: shows the tooltip of a control when it is pointed at or takes focus from the keyboard, and hides it on leave, blur, press and Esc. It opens it with `showPopover({ source })`, so the stylesheet places it. | Firefox and Safari have interest invokers. |
| `FlDialog`, `FlDrawer`, `FlCommandPalette`, the drawer of `FlAppShell`, `FlMenu`, `FlPopover` | Focus back to the opener when the browser left it on `<body>`. The opener is the `source` of the `toggle` event, which every engine reports. This covers the pointer opening in WebKit, and the rule of section 5 of the plan for an opener or an overlay that is removed while open. The focus ring shows only when the overlay was closed from the keyboard, as `:focus-visible` decides. | A real Safari shows that it is not needed there. |

### What the script covers because no browser has it

| Component | What the script adds |
| --- | --- |
| `FlMenu`, `FlSplitButton` | `aria-expanded` on the button, from the `toggle` event. Focus on the first item on open. Arrow up and down, Home and End between the items, and a typed letter. Arrow down on the button opens. Tab closes. A press on an item closes the menu; on a checkbox or radio item it sets `aria-checked` and the menu stays. |
| `FlPopover` | `aria-expanded` on the button, from the `toggle` event. |
| `FlTabs` | Roving `tabindex`, the arrow keys in the direction of the text, Home and End, the selection that follows focus, and the panel that shows. |
| `FlTree` | Roving `tabindex`, the arrow keys, Home and End, opening and closing a branch, and selection. |
| `FlTable` | Focus that moves between rows with the arrow keys, Space that selects the row, and the mixed state of the checkbox in the header. |
| `FlCombobox` | Opening and closing the list as a manual popover, the active option under the arrow keys with `aria-activedescendant`, Enter, Esc and Backspace, keeping focus in the input when the list is pressed, and closing when focus leaves. |
| `FlCommandPalette` | The shortcut that opens it, the active option under the arrow keys, and Enter. |
| `FlToastRegion` | Showing the region as a manual popover, showing it again when a dialog opens after it so that it stays on top, and the time of a toast, which stops while it is pointed at or holds focus. |
| `FlNumberInput` | The two step buttons, which call `stepUp()` and `stepDown()` and are disabled at the limits. |
| `FlFileDropZone` | `data-fl-dragover` while files are held over the area. |
| `FlSortableList` | The move buttons, the handle that lifts an item for the arrow keys and Esc, and the words for the live region. |
| `FlAppShell`, `FlNavigation` | The sidebar as a rail, a group that opens the sidebar when it is opened in the rail, and closing the drawer when the window grows wide. |
| `FlBreadcrumb` | Showing the levels that a long trail left out, and focus on the first of them. |
| `FlCheckbox` | The `indeterminate` property, which has no attribute. |
| Search field (a pattern) | The clear action, and the shortcut that brings focus to the global search. |

For the Interactive components (table, combobox, tree, file drop zone, sortable list, command palette, toasts) the script owns the keys, the focus and the attributes that must change at once; the data, the filter and the selection stay in .NET, as point 5 of the policy says. Where exactly that line runs is settled in the issue of each component.

### What is accepted without script

- An overlay closes without an exit transition in Firefox and in WebKit. The stylesheet already asks for it, so it arrives by itself when those browsers implement what Chromium has.
- The tabs have no fade in a Firefox older than 159.
- The parts of a native date or time input that the stylesheet cannot reach are drawn by the browser.

## Consequences

- The script is smaller than section 4.3 expected. It holds no positioning code, no dismissal code and no stand-in for the dialog, which were the parts most likely to go wrong.
- The tooltip and the focus return run different code in Chromium than in Firefox and WebKit. Their browser tests have to run in all three engines, and the nightly run of section 7 of the plan is where a regression would otherwise hide for a day. These two tests are candidates for every pull request.
- The library depends on the last two versions for real. A browser from before these features loses the placement of every overlay, the commands of every dialog and the light dismiss of `closedby` at once, and the script will not catch it. The guide for hosts has to say so.
- The two fallbacks have an end. When Firefox and Safari ship interest invokers, the tooltip code is deleted, and the "Until" column above is what to check at each release.
- Section 4.3 of the plan still lists the two items that this record removes. The plan is to be brought in line when it is next edited; until then this record is the newer word.
- **A pass in real Safari is owed**, on macOS and on iOS, before the overlays are called done in milestone C3. It has to look at: the native date and time inputs and the icon of their button, focus after an overlay closes, `overscroll-behavior` in the lists of overlays, the handle of a text area, the tooltip under touch, and the place of an anchored popover while the on-screen keyboard is up.
- **A toast under a modal dialog cannot be pressed**, in any browser. The issue of `FlToastRegion` has to settle it, for example by moving the region into the dialog that is open, and the "showing it again" item above changes with that choice.
- **The field on a `<fieldset>` needs another way to place its legend** before `FlField` is built in milestone C1, since no rule on the legend alone was found that holds in all three engines. A wrapper inside the fieldset, or a group with `aria-labelledby`, are the two candidates.
- The pickers of the native date and time inputs, and what a screen reader says in each overlay, remain for the pass by hand of section 7.
