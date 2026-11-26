# FluentCMS Components

An opinionated, minimal Blazor component library: the user interface layer of the [FluentCMS](https://github.com/fluentcms/FluentCMS) admin, usable with or without the CMS.

> **Planning.** Nothing is built yet. The [implementation plan](docs/planning/implementation-plan.md) says what the library is, how it is built and tested, and in which order.

## What it will be

- **One look.** The FluentCMS design system, themed through tokens: accent, radius, density, typeface, light and dark.
- **Native first.** Built on HTML elements such as `<dialog>`, `popover` and `<select>`, with one small script and no third-party dependency.
- **Every render mode.** Static server-side rendering, interactive server and interactive WebAssembly.
- **Accessible by default.** WCAG 2.2 AA, full keyboard use, right-to-left layout.
- **Small.** Fewer than 40 components, prefixed `Fl`, in the package `FluentCMS.Components`.
- **Designed from scratch.** Its components and API come from the design system and from HTML, not from an earlier library.
