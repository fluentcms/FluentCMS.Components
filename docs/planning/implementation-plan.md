# FluentCMS Components: implementation plan

Status: draft for review, 27 November 2026. The decision records, the design system of section 6, the stylesheet and the gallery are built; no component is.

FluentCMS Components is an opinionated, minimal Blazor component library. It is the user interface layer of the [FluentCMS](https://github.com/fluentcms/FluentCMS) admin, and it can be used without the CMS. This document says what the library is, how it is built and tested, in which order, and how the FluentCMS admin is rebuilt on it.

## 1. Why a library of its own

- **A new design.** The admin gets a custom design, chosen from four static mockups in November 2026. Its interface so far was a working test of the Management API, not a designed product.
- **A clean start.** The library is designed from the design system and the HTML platform. It does not continue `FluentCMS.Admin.Components`, the set of `Cms*` wrappers around Fluent UI Blazor that the admin uses today: no code, no API shape and no component list is taken from it.
- **The cost of a component change.** In the FluentCMS repository every change waits for the full CI, which runs the API, three databases and the end-to-end tests. A component library with its own tests and its own release does not.
- **Use beyond the admin.** The same components serve static server-side rendering, interactive server and WebAssembly, so other Blazor apps can use them, and the hosted sign-in pages of FluentCMS can share the look.

## 2. Decisions

### 2.1 Settled

| # | Decision |
| --- | --- |
| D1 | The library is named FluentCMS Components. Repository `fluentcms/FluentCMS.Components`, package `FluentCMS.Components`, namespace `FluentCMS.Components`. |
| D2 | Components and public types carry the prefix `Fl` (`FlButton`). CSS classes carry `fl-`, custom properties `--fl-`, data attributes `data-fl-`. |
| D3 | The library is designed and written from scratch. Its component list and its API come from the design system (section 6) and from what HTML offers, not from the `Cms*` set. |
| D4 | Native HTML, its own stylesheet, one small script. No third-party component library and no runtime package dependency beyond the framework's `Microsoft.AspNetCore.Components.Web`. |
| D5 | Every component works in static server-side rendering, interactive server and interactive WebAssembly. The library never sets a render mode. |
| D6 | The library is opinionated and minimal (section 3). |
| D7 | The library lives in its own repository with its own tests, CI and releases, and is published on nuget.org. |
| D8 | The FluentCMS admin and the admin halves of plugins use `Fl*` directly. `FluentCMS.Admin.Components` is removed; no wrapper layer replaces it. |
| D9 | The visual direction is the selected admin mockup: a grouped sidebar, a header with global search, bordered inputs, data tables and a blue accent. It becomes the FluentCMS design system. The mockup is a draft: its direction is settled, and its details may change with later requirements. |
| D10 | The stylesheet is plain CSS: no CSS framework, no preprocessor and no build tool beyond MSBuild joining the files (section 4.2). |
| D11 | The repository is public, since 26 November 2026, and the library is under the MIT license, as FluentCMS is. |
| D12 | The icon family is a curated subset of Lucide (ISC license), embedded as SVG paths. It is the style of the reference design, one drawing serves every size, and its stroke weight is a token. Settled on the design system page, where it stands beside Fluent System Icons. |
| D13 | The typeface is the system font stack by default. The family is one token, `--fl-font-family`, so a host can set Inter and serve it; the library bundles no font. |
| D14 | Date and time inputs are the browser's own `date`, `time` and `datetime-local` inputs in the box of the text input. A range is two inputs. A custom calendar is not part of the library; it would be a later component, only for a range in one control or a format fixed by the application. What the native control cannot do is listed on the forms page of the design system. |
| D15 | Both candidates are in. The command palette is what the global search of the app shell opens, on every screen. The sortable list orders the columns of a list and the ordered fields of an editor; it stays one level and one list, and its move buttons work without dragging. |
| D16 | The package targets `net10.0` only, as FluentCMS. A second target is added when someone asks for it. |
| D17 | A preview is published on a maintainer's tag (`v0.3.0-preview.2`) and never on a merge, since a version on nuget.org can be unlisted but never deleted. The local source switch of section 9.3 covers daily work. |
| D18 | The gallery is hosted on GitHub Pages of this repository, as the WebAssembly build. The other two render modes run in CI and locally. |
| D19 | The package requires `Microsoft.AspNetCore.Components.Web` 10.0.0, so that a host on any 10.0 patch is not pushed to a newer one. The gallery and the tests run on the newest patch. |

### 2.2 Open, with a recommendation

| # | Question | Recommendation |
| --- | --- | --- |
| O7 | Scope of the table. | Columns, external sort with several keys, row selection, loading and empty states, keyboard movement between rows. No virtual scrolling, no inline editing, no column resizing. |
| O10 | Version at which the library is stable. | `1.0.0` before FluentCMS `1.0.0`, which freezes its contract; plugin authors compile against both. |

## 3. What opinionated and minimal mean

These rules decide what gets in. A proposal that breaks one is declined, or changes this section first.

1. **One look.** A host changes the accent, the radius, the density, the typeface and the theme (light or dark) through tokens. There are no alternative skins and no per-component style parameters.
2. **A class where a class is enough.** Something becomes a component only when it owns behaviour, structure or accessibility wiring. A stack, a page header, a toolbar, a divider, a keyboard key, a skeleton and a link are documented CSS classes, not components.
3. **Native first.** A component is built on the HTML element that already does the job: `<dialog>`, the `popover` attribute, `<details>`, `<select>`, `<input type="date">`. Script only adds what the platform lacks.
4. **An API of its own.** Parameters follow HTML's names and the needs of the three render modes. Nothing mirrors another library, and nothing is kept for compatibility with `Cms*`.
5. **Few parameters.** A component exposes what a back-office screen needs. There is no pass-through of arbitrary templates for every inner part.
6. **Defaults that are not optional.** WCAG 2.2 AA, full keyboard use, right-to-left layout, reduced motion, forced colours, and trimming for WebAssembly.
7. **Earned by the design.** A component enters when at least two screens of the design system need it, or when leaving it out would make a host rebuild a hard accessibility pattern by hand (a combobox, a tree).

### Not in scope

- Charts, a scheduler, a rich text editor, a page layout system, a utility-class framework.
- Blazor Hybrid and MAUI. Nothing prevents them, nothing tests them.
- A form validation framework. Inputs work with `EditForm` and show its messages.
- CMS concepts: relation pickers, media pickers, field editors, status logic. Those stay in the FluentCMS admin, built from these components.
- Browsers older than the last two versions of Chrome, Edge, Firefox and Safari.

## 4. Architecture

### 4.1 Repository layout

```text
src/FluentCMS.Components/             the library: components, wwwroot/fluentcms-components.css, the script
gallery/Gallery/                      the host of the gallery: static SSR and interactive server
gallery/Gallery.Client/               the gallery's pages and its WebAssembly build
tests/FluentCMS.Components.Tests/     component tests (bUnit)
tests/FluentCMS.Components.Browser/   browser tests (Playwright, axe-core) against the gallery in each render mode
design/                               the design system and the reference design (static HTML and CSS), and their tools
docs/                                 planning, decisions, guides
```

The gallery is the documentation site and the test target at once. It holds one page per component, with every variant and state, and the reference screens of section 4.8. A switch on each page chooses the render mode, the theme, the density and the direction.

### 4.2 Styling

- **One class-based stylesheet**, `fluentcms-components.css`, served as a static web asset. Components render plain elements with `fl-` classes, so plain HTML with the same classes looks the same, and Razor Pages can use it without Blazor. Blazor's CSS isolation is not used, since its scope attributes tie the styles to the components.
- **Plain CSS, with no framework, no preprocessor and no build tool.** A library has to sit beside whatever its host uses. Bundling Bootstrap or Tailwind would clash with a host's own copy, and their global resets would restyle the host's pages. Utility classes would also end the plain-HTML use above, and would bring a Node toolchain into a .NET repository. Nesting, custom properties, cascade layers, `:has()`, container queries and `color-mix()` are in every target browser, and cover what those frameworks were needed for. The stylesheet is written as one file per component and joined at build by MSBuild. Sass is left out for the same reason of weight: its variables cannot change at run time as themes need, and nesting is native. What it still offers alone, mixins and loops, is replaced by tokens set per variant and by grouped selectors. The choice is cheap to reverse, since plain CSS is valid SCSS and the reverse is not: a compiler or a minifier is added as a build-time step only when the size check or real repetition asks for it. The design system page and the gallery load the same stylesheet the package ships, with no translation between them.
- **Tokens** are CSS custom properties in two tiers: global (`--fl-color-accent`, `--fl-space-3`, `--fl-radius-m`, `--fl-font-family`) and per component where a component needs its own (`--fl-button-height`). Components read tokens only; no colour or size is written into a component rule.
- **Cascade layers** (`fl.base`, `fl.tokens`, `fl.components`) keep the library's rules below a host's own styles without specificity fights.
- **Themes and density** are attributes on any ancestor: `data-fl-theme="light|dark"`, following `prefers-color-scheme` when absent, and `data-fl-density="comfortable|compact"`.
- **Logical properties only** (`margin-inline-start`), so `dir="rtl"` mirrors the layout with no second stylesheet.
- **No inline script and no injected `<style>` element**, so the library runs under a content security policy that allows scripts from the site alone, as the FluentCMS admin's does. Avoiding inline `style` attributes as well, so that a host can also refuse inline styles, is a goal tracked per component.

### 4.3 Behaviour and script

- **One script**, a JavaScript initializer of the package (`FluentCMS.Components.lib.module.js`), which Blazor loads in every render mode with no setup by the host. It enhances the HTML that is already on the page, found by `data-fl-*` attributes, and applies again after an enhanced navigation.
- **It adds only what HTML lacks**: roving focus in tabs, menus and trees, focus return where a browser leaves it on the page, the tooltip where a browser has no interest invokers, and the keys and states that no browser offers. The placement of a popover and the dismissal rules of overlays are native in every target browser and are not in the script.
- **No call from .NET per interaction.** In the interactive modes .NET state is the source of truth and the script reports changes through DOM events the component listens to. No synchronous interop, which exists in WebAssembly only.
- **The spike of C0** checked `<dialog>`, `popover`, anchor positioning, invoker commands and the other recent features the design system leans on, in Chromium, Firefox and WebKit builds. Its result, and the list of what the script covers for each component, is [decision record 0003](../decisions/0003-script-policy.md). Real Safari was not available to it.

### 4.4 Render modes

Each component declares one of three levels, shown on its gallery page.

| Level | Meaning | Examples |
| --- | --- | --- |
| Static | Markup and CSS. Works fully with no code running. | button as a link or a submit, card, badge, navigation, breadcrumb, pagination, form inputs inside a form |
| Native | Behaviour from native elements and the script. Works fully in static SSR. | dialog, drawer, menu, popover, tooltip, tabs, disclosure |
| Interactive | Needs an interactive mode for its full function, and renders a documented static fallback. | table with sort and selection callbacks, combobox, file drop zone with progress, tree with lazy loading, toasts raised from code |

Rules for every component: no `@rendermode` inside the library; no service that exists in one mode only; nothing done in `OnInitialized` that needs the browser; prerendering followed by interactivity must not flash, so the interactive render produces the markup the prerender produced, ids included. The framework's swap of the prerendered DOM loses the focus and the text typed into an input before it, and the library does not restore either. A host that cannot accept that turns prerendering off for that page or component. See the [update of decision record 0001](../decisions/0001-render-mode-contract.md#update-focus-across-prerendering).

### 4.5 Forms

Inputs bind with `@bind-Value`, work inside an `EditForm` with its validation messages, and work without one. In static SSR they post as ordinary form fields with stable names. `FlField` owns the label, the help text, the error, the required mark and the character counter, and wires `aria-describedby` and `aria-invalid`. A decision record in C0 settles the base class.

### 4.6 Services and texts

- `AddFluentCmsComponents()` registers what the library needs.
- `IFlToastService` raises toasts; `FlToastRegion` renders them inside two live regions, a polite one and an assertive one for errors, which stay until dismissed.
- `IFlTextProvider` supplies the library's own texts (Close, Loading, Sort ascending). English is built in; a host returns its translations.

### 4.7 Quality rules built into the project

- Warnings are errors. The trim and AOT analyzers are on; the package is trimmable.
- The public API is tracked with `Microsoft.CodeAnalysis.PublicApiAnalyzers`. From `1.0.0`, package validation compares each release with the one before.
- A size check in CI reports the compressed stylesheet, script and assembly, and fails past a budget set at the end of C0 from the first measurements.

### 4.8 Reference screens

Four screens are built in the gallery from the library's components alone, with sample data: a settings form, a dashboard, a content list and an entry editor. The last three are the screens of the selected mockup. They are the library's own integration test, since the library no longer leans on an existing consumer to find its gaps:

- each works in the three render modes and passes the accessibility checks of section 7;
- a missing parameter or a wrong default shows there first, and is fixed before the FluentCMS admin meets it;
- they double as the examples of the documentation.

## 5. Component inventory

The list comes from the screens of the selected design, and the design system (section 6) is its source of truth: a component it does not need leaves this table, and one it needs enters under rule 7. The level is that of section 4.4. The table is as the design system and its four reference screens left it.

| Group | Component | Built on | Level | Milestone |
| --- | --- | --- | --- | --- |
| Foundations | `FlIcon` | inline SVG | Static | C0 |
| Foundations | `FlThemeProvider` | wrapper with data attributes | Static | C1 |
| Actions | `FlButton` | `<button>`, `<a>` | Static | C0 |
| Actions | `FlSplitButton` | button and menu | Native | C3 |
| Actions | `FlMenu`, `FlMenuItem` | `popover` | Native | C3 |
| Forms | `FlField` | `<label>` | Static | C1 |
| Forms | `FlTextInput` | `<input>` | Static | C1 |
| Forms | `FlTextArea` | `<textarea>` | Static | C1 |
| Forms | `FlNumberInput` | `<input>` | Static | C1 |
| Forms | `FlSelect` | `<select>` | Static | C1 |
| Forms | `FlCheckbox` | `<input type="checkbox">` | Static | C1 |
| Forms | `FlRadioGroup` | `<input type="radio">` | Static | C1 |
| Forms | `FlSwitch` | `<input type="checkbox" role="switch">` | Static | C1 |
| Forms | `FlSegmented` | radio group | Static | C1 |
| Forms | `FlDateInput`, `FlTimeInput` | `<input type="date">`, `<input type="datetime-local">`, `<input type="time">` (D14) | Static | C1 |
| Forms | `FlCombobox` | combobox pattern, `popover` | Interactive | C3 |
| Forms | `FlFileDropZone` | `<input type="file">` | Interactive | C4 |
| Data | `FlBadge` | `<span>` | Static | C1 |
| Data | `FlTag` | `<span>`, `<button>` | Static | C1 |
| Data | `FlAvatar` | `<span>` | Static | C1 |
| Data | `FlTable`, `FlColumn` | `<table>` | Interactive | C2 |
| Data | `FlPagination` | `<nav>` | Static | C2 |
| Data | `FlEmptyState` | `<div>` | Static | C2 |
| Data | `FlProgress` | `<progress>` | Static | C2 |
| Data | `FlTree` | tree pattern | Interactive | C4 |
| Data | `FlSortableList` | list with move controls and drag (D15) | Interactive | C4 |
| Layout and navigation | `FlCard` | `<section>` | Static | C1 |
| Layout and navigation | `FlAppShell` | grid, drawer on narrow windows | Native | C2 |
| Layout and navigation | `FlNavigation`, `FlNavigationSection`, `FlNavigationLink` | `<nav>`, `<details>` | Native | C2 |
| Layout and navigation | `FlBreadcrumb` | `<nav>`, `<ol>` | Static | C2 |
| Layout and navigation | `FlTabs`, `FlTab` | tablist pattern | Native | C2 |
| Layout and navigation | `FlDisclosure` | `<details>` | Native | C2 |
| Overlays and feedback | `FlAlert` | `<div role>` | Static | C2 |
| Overlays and feedback | `FlDialog` | `<dialog>` | Native | C3 |
| Overlays and feedback | `FlDrawer` | `<dialog>` | Native | C3 |
| Overlays and feedback | `FlPopover` | `popover` | Native | C3 |
| Overlays and feedback | `FlTooltip` | `popover` | Native | C3 |
| Overlays and feedback | `FlToastRegion`, `IFlToastService` | live regions | Interactive | C3 |
| Overlays and feedback | `FlCommandPalette` | `<dialog>`, listbox pattern (D15) | Interactive | C4 |

That is 39 components. CSS-only patterns, documented in the design system and in the gallery: stack and cluster, page header, toolbar, divider, keyboard key, skeleton, link, search field, description list, stat tile, timeline.

What the design system fixed about this list:

- The text, number, date and time inputs and the search field share one box, `fl-input`, which holds the icon, the affixes and the action. Each adds only its own parts.
- The section and the link of the navigation, the tab of the tabs and the item of the menu are parts of their parent in the stylesheet, not blocks of their own. The child components render those parts.
- The four reference screens needed layout rules that the library does not have yet. They are in `design/system/screens/screens.css`, each with what would make it unnecessary, and they are the candidates for the next patterns and parts: a page container, a main and side column layout, a bar of the shell that stays in view, a header whose search and user button fold, a text part on the avatar, an outline badge, actions at the end of the tabs, a width modifier on the input, and a sortable item that holds a disclosure.

### Rules every overlay and control follows

- A dialog and a drawer open on their first control, have a close button named Close unless told otherwise, and give focus back to their opener, also when removed from the page while open.
- A toast is read through a live region; an error is assertive and stays until dismissed.
- Every target is at least 24 by 24 CSS pixels, and no action is reachable by hover alone.
- Every page reflows at 320 CSS pixels, and nothing moves under reduced motion.

## 6. The design system

Static HTML and CSS in `design/system/`, made before any component is written. It is the visual contract the components are built and reviewed against, and the source of the inventory.

- **Tokens**: colour (neutrals, accent, the status colours, both themes), type scale, spacing scale, radius, borders, elevation, motion, focus ring, the two densities.
- **Every component of section 5** with its variants, sizes and states: rest, hover, focus, pressed, disabled, read-only, invalid, loading, selected, empty.
- **The CSS-only patterns.**
- **The reference screens** of section 4.8, composed from the parts, to prove they hold together.
- **Light, dark and right-to-left** views of all of it.

It settled the icon family, the typeface, the date and time inputs and the two candidates (D12 to D15). Its CSS is the first draft of the library's stylesheet: the tokens and the component files are copied into the library as they are.

```text
design/system/index.html                              overview and the tokens
design/system/RECIPE.md                               how a component is drawn and specified
src/FluentCMS.Components/Styles/tokens.css            the tokens
src/FluentCMS.Components/Styles/base.css              layers, base rules, stack and cluster
src/FluentCMS.Components/Styles/components/<name>.css one file per component
src/FluentCMS.Components/Styles/patterns/<name>.css   one file per CSS-only pattern
design/system/foundations.html                        icon, button, field, text input and the base patterns
design/system/forms.html                              the form controls
design/system/actions-overlays.html                   split button, menu, overlays, alert, toast, command palette
design/system/data.html                               badge, tag, avatar, table, pagination, tree and the data patterns
design/system/layout-navigation.html                  card, app shell, navigation, breadcrumb, tabs, disclosure
design/system/screens/                                the four reference screens, their layout rules and their script
design/system/icons/lucide.svg                        the icon set
design/system/docs/                                   the chrome of the pages; never the library's
design/tools/contrast.mjs                             checks the colour tokens against WCAG AA
design/tools/icons.mjs                                builds the icon set from a pinned version
```

## 7. Testing

Every test is written new, for this library.

| Layer | Tool | What it proves | Runs |
| --- | --- | --- | --- |
| Component tests | bUnit, xUnit | markup, parameters, ARIA attributes, events, texts through `IFlTextProvider` | every pull request |
| Browser tests | Playwright for .NET against the gallery | behaviour in static SSR, interactive server and WebAssembly; keyboard-only use of each pattern; focus management | every pull request, Chromium |
| Accessibility | axe-core (`Deque.AxeCore.Playwright`) on every gallery page and reference screen | no violation in light, dark and right-to-left; target sizes; reflow at 320 px; no motion under reduced motion | every pull request, Chromium |
| Other engines | the same browser tests | Firefox and WebKit | nightly |
| Visual regression | screenshots of the gallery pages | unintended changes of the look | from C4, when the look is stable |

The tools are the ones FluentCMS already uses, so contributors meet nothing new. A pull request's checks are meant to end within ten minutes; a job that passes that is split or moved to the nightly run.

A screen reader pass (NVDA and VoiceOver) of every Native and Interactive component is scripted in `docs/` and run by hand before each minor release, since no test covers it.

## 8. CI and releases

- **CI** on every pull request: build with warnings as errors, component tests, browser tests in the three render modes, pack, size check, markdownlint.
- **Gallery**: built as WebAssembly and deployed to GitHub Pages on each merge to `main` (D18).
- **Previews**: a tag `v0.3.0-preview.2` publishes that version to nuget.org, and a merge never does (D17).
- **Releases**: a tag `v0.3.0` publishes the stable version and creates the GitHub release with its notes.
- **Signing in to nuget.org** uses trusted publishing, as FluentCMS does. The maintainer adds a trusted publishing policy for this repository on nuget.org and the `NUGET_USER` secret of a `release` environment.
- **Versioning** is semantic. Before `1.0.0` a minor version may break the API, and says so in its notes. The version comes from the tag; no versioning tool is added.
- **Working rules** are those of FluentCMS: every change starts from an issue, a branch per issue, a pull request into `main`, short imperative commit titles, decision records in `docs/decisions/`.

## 9. Rebuilding the FluentCMS admin

The library is finished to a feature-complete preview first (C0 to C4). The admin is then rebuilt on it, in the FluentCMS repository (C5). The library does not wait for the admin, and does not bend to its present pages.

### 9.1 What changes there

| Area | Change |
| --- | --- |
| Decision | A new decision record replaces decision 26 (Fluent UI Blazor behind wrappers) and section 2.3 of the admin technical specification. |
| Pages | The admin's screens are rebuilt to the new design with `Fl*`. This is new markup, not a rename: the routes, the API calls, the state and the access rules of each page stay, and its Razor markup and styles are written again. The old layer is used 863 times in 115 Razor files, measured on 26 November 2026, which is the size of what is replaced. |
| Packages | `FluentCMS.Components` is referenced at an exact version. `FluentCMS.Admin.Components`, `Microsoft.FluentUI.AspNetCore.Components` and its icon package are removed at the end. |
| Seams | Texts go through `IFlTextProvider`, answered from the admin's resource files. Toasts go through `IFlToastService`, with `FlToastRegion` in the shell. The brand accent of `config.json` sets `--fl-color-accent`. |
| Tests | Page tests, the accessibility walk of every view and the keyboard loop stay, and are the acceptance test of each slice. The tests of the old wrappers are deleted with them. |
| Architecture tests | The rules about the wrapped set are replaced by one rule: an admin assembly references no user interface library but `FluentCMS.Components`. The allow-list of JavaScript interop loses the old library's entries. |
| Plugin authors | The plugin template and the plugin guide use `Fl*`. The release notes carry a table from each `Cms*` component to the nearest `Fl*` one, written at that time. This is a breaking change and lands before `1.0.0`. |
| Gallery | The page at `/system/components` is removed; the library's gallery replaces it. |
| Download size | The first-download budget is measured again and lowered to the new size. |

### 9.2 In slices, on one feature branch

The old and the new layer live side by side on a feature branch of FluentCMS only; `main` gets the result when the last slice is done and the old layer is gone.

| Slice | Admin area |
| --- | --- |
| S1 | Shell: layout, sidebar, header, site switcher, user menu, breadcrumbs, and the new dashboard |
| S2 | Lists: content list, filters, saved views, bulk actions and progress, record lists of administration, toasts |
| S3 | Forms: content form with its inspector, field editors, schedule, version history, rich text, blocks |
| S4 | Administration, account and settings pages; the media library |
| S5 | The six admin halves of plugins, the sample plugins of the tests, the plugin template and guide |

### 9.3 Working on both repositories at once

FluentCMS restores packages from nuget.org only, so a component fix would otherwise need a published preview before the admin can use it. An opt-in MSBuild property replaces the package reference with a project reference to a local checkout:

```text
dotnet build -p:FluentCmsComponentsSource=../FluentCMS.Components
```

Without the property the build uses the pinned package, so CI and other contributors are not affected. The feature branch moves to a new preview at the end of each slice.

## 10. Milestones

| Milestone | Content | Done when |
| --- | --- | --- |
| C0 Foundation | This plan, the decision records, the design system page, the repository's scaffolding, the gallery in three render modes, the test harness, CI, the token stylesheet, the script, `FlIcon` and `FlButton` | `0.1.0-preview.1` is on nuget.org, the gallery is online, and `FlButton` passes every gate of section 11 |
| C1 Primitives and forms | Theme provider, badge, tag, avatar, card, the CSS patterns, field and every input but the combobox; forms in static SSR | The settings form reference screen passes in the three render modes |
| C2 Structure and data | App shell, navigation, breadcrumb, tabs, disclosure, table, pagination, empty state, alert, progress | The dashboard and the content list reference screens pass |
| C3 Overlays and feedback | Overlay base, dialog, drawer, menu, popover, tooltip, split button, toasts, combobox | The entry editor reference screen passes, and the list has its bulk actions and filters |
| C4 Complete preview | Tree, file drop zone, command palette, sortable list; visual regression tests; the guides; the screen reader pass | A feature-complete preview is published, with every component documented |
| C5 The admin rebuilt | Slices S1 to S5 in FluentCMS; the old layer and Fluent UI Blazor removed; the feature branch merged | FluentCMS `main` runs on the library, and a release candidate of the library is published |
| C6 Stable | API review and freeze, package validation | `1.0.0` is published, before FluentCMS `1.0.0` |

No dates are given. The order is fixed; C1 to C3 can overlap where two people work.

## 11. Definition of done for a component

1. It is on the design system page with all its states, and matches it.
2. Its rules read tokens only, use logical properties, and pass in light, dark, right-to-left, compact density and forced colours.
3. It works in static SSR, interactive server and WebAssembly at its declared level, and the gallery page says which level.
4. Keyboard use follows the matching pattern of the ARIA Authoring Practices, with a browser test that uses the keyboard alone.
5. axe-core reports nothing on its gallery page, targets are at least 24 by 24 pixels, and it reflows at 320 pixels.
6. Component tests cover its parameters, its ARIA attributes and its texts.
7. Its gallery page shows every variant, a parameter table and a copyable example.
8. Its public API is recorded, the size check passes, and no dependency was added.

## 12. Proposed issues

Sizes are S (hours), M (a day or two) and L (several days). FluentCMS issues are marked "in FluentCMS".

### C0 Foundation

| ID | Issue | Size |
| --- | --- | --- |
| C-01 | Scaffolding: solution, build properties, central package versions, `.editorconfig`, `.gitignore`, `.gitattributes`, `nuget.config`, contributing guide, markdownlint | S |
| C-02 | Decision records: the render mode contract, the styling architecture, the script policy, the forms model | M |
| C-03 | Design system page (section 6), made from the reference design in `design/reference/admin.html` | L |
| C-04 | Token stylesheet: tokens, both themes, both densities, base rules, layers | M |
| C-05 | Gallery that runs every page in static SSR, interactive server and WebAssembly, with theme, density and direction switches | M |
| C-06 | Test harness: the bUnit project, and the browser project with the render mode matrix, axe-core, a keyboard-only helper and the WCAG 2.2 checks | L |
| C-07 | CI workflow under ten minutes, with the size report | M |
| C-08 | The script as a JavaScript initializer, after the policy of decision record 0003; the spike is done | M |
| C-09 | `IFlTextProvider` with the English texts | S |
| C-10 | `FlIcon` and the icon set (D12) | M |
| C-11 | `FlButton` through every gate of section 11 | M |
| C-12 | Publish workflow with trusted publishing, the gallery on GitHub Pages, `0.1.0-preview.1` | M |

### C1 Primitives and forms

| ID | Issue | Size |
| --- | --- | --- |
| C-13 | `FlThemeProvider`: theme, density, direction, accent | S |
| C-14 | The CSS patterns: stack and cluster, page header, toolbar, divider, keyboard key, skeleton, link, description list, stat tile, timeline | M |
| C-15 | `FlBadge`, `FlTag`, `FlAvatar` | M |
| C-16 | `FlCard` | S |
| C-17 | `FlField` and `FlTextInput`, with the search field pattern | M |
| C-18 | `FlTextArea` and `FlNumberInput` | M |
| C-19 | `FlSelect` | S |
| C-20 | `FlCheckbox` with the mixed state, `FlRadioGroup`, `FlSwitch` | M |
| C-21 | `FlSegmented` | S |
| C-22 | `FlDateInput` and `FlTimeInput` (D14) | M |
| C-23 | Forms end to end: `EditForm`, validation messages, a form post in static SSR, across every input | M |
| C-24 | Reference screen: the settings form | M |

### C2 Structure and data

| ID | Issue | Size |
| --- | --- | --- |
| C-25 | `FlDisclosure` | S |
| C-26 | `FlNavigation`, its sections and links, with collapsible groups, counts and badges | M |
| C-27 | `FlAppShell`: sidebar, header, main, the narrow layout, the skip link | M |
| C-28 | `FlBreadcrumb` | S |
| C-29 | `FlTabs` and `FlTab` | M |
| C-30 | `FlTable` and `FlColumn` (O7) | L |
| C-31 | `FlPagination` | S |
| C-32 | `FlAlert`, `FlEmptyState`, `FlProgress` | M |
| C-33 | Reference screens: the dashboard and the content list | L |

### C3 Overlays and feedback

| ID | Issue | Size |
| --- | --- | --- |
| C-34 | Overlay base on `<dialog>` and `popover`: focus, dismissal, placement | L |
| C-35 | `FlDialog`: sizes, footer, the confirmation pattern | M |
| C-36 | `FlDrawer`, and the app shell's narrow navigation on it | M |
| C-37 | `FlMenu` and `FlMenuItem`, with checked and radio items; `FlSplitButton` | M |
| C-38 | `FlPopover` and `FlTooltip` | M |
| C-39 | `IFlToastService` and `FlToastRegion` | M |
| C-40 | `FlCombobox`, single and multiple, with its static fallback | L |
| C-41 | Reference screen: the entry editor; bulk actions and filters on the content list | L |

### C4 Complete preview

| ID | Issue | Size |
| --- | --- | --- |
| C-42 | `FlTree`, with lazy loading and its static fallback | L |
| C-43 | `FlFileDropZone`, with its static fallback | M |
| C-44 | `FlCommandPalette` and `FlSortableList` (D15) | L |
| C-45 | Visual regression tests of the gallery | M |
| C-46 | Guides: getting started, theming, render modes, forms | M |
| C-47 | The screen reader script and its first full pass | M |

### C5 The admin rebuilt

| ID | Issue | Size |
| --- | --- | --- |
| C-48 | In FluentCMS: the decision record, the package reference, the local source switch, the feature branch | M |
| C-49 | In FluentCMS: slice S1, the shell and the dashboard | L |
| C-50 | In FluentCMS: slice S2, the lists | L |
| C-51 | In FluentCMS: slice S3, the forms, field editors and rich text | L |
| C-52 | In FluentCMS: slice S4, administration, account, settings and the media library | L |
| C-53 | In FluentCMS: slice S5, the six admin halves, the sample plugins, the plugin template and guide | L |
| C-54 | In FluentCMS: delete `FluentCMS.Admin.Components` and the Fluent UI packages; update the architecture tests and the admin specification | M |
| C-55 | In FluentCMS: the accessibility walk, the keyboard loop and the screen reader script across the admin; the first download measured again | M |
| C-56 | In FluentCMS: release notes with the table for plugin authors; merge the feature branch | M |
| C-57 | Optional, in FluentCMS: the hosted sign-in and account pages use the stylesheet | M |

### C6 Stable

| ID | Issue | Size |
| --- | --- | --- |
| C-58 | API review, the public API files moved to shipped, package validation turned on | M |
| C-59 | `1.0.0` | S |

## 13. Risks

| Risk | What limits it |
| --- | --- |
| A library designed without its consumer meets the real admin late, and its API has gaps. | The reference screens of section 4.8 are real screens, not samples; a short spike at the end of C2 rebuilds the admin's shell on a throwaway FluentCMS branch; before `1.0.0` the API may still change. |
| Static SSR makes the behaviour-heavy components slow to build. | Native elements first; the spike, which found the overlays native in every target browser (decision record 0003); the three levels of section 4.4 let a component ship with a documented fallback. |
| A native feature is missing in one of the target browsers. | The spike names them (interest invokers outside Chromium); the script covers the gap; the nightly run in Firefox and WebKit watches for regressions; real Safari is still to be checked on a Mac. |
| The admin's accessibility gets worse than it is today. | The rules of section 5 are tests here; the admin's own accessibility walk and keyboard loop gate every slice; the screen reader pass before each minor release. |
| Rebuilding every admin screen is more work than renaming components. | It is the cost of the new design, which needs new markup in any case; the slices keep each step reviewable. |
| Two repositories slow daily work. | The local source switch of section 9.3; previews by tag. |
| The set grows past minimal. | Section 3, rule 7; every addition names the two screens that need it. |
| The rebuild collides with FluentCMS `1.0.0`. | The breaking change for plugin authors must land before the contract freezes; if it cannot, `1.0.0` waits or the rebuild is cut to a later major version. This is the maintainer's call at the end of C3. |
| `FluentCMS.Components` is taken for `FluentCMS.Admin.Components`. | The old package gets a deprecation notice on nuget.org that points to the new one when C5 ends. |
