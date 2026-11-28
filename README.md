# FluentCMS Components

An opinionated, minimal Blazor component library: the user interface layer of the [FluentCMS](https://github.com/fluentcms/FluentCMS) admin, usable with or without the CMS.

> **State.** The first component, `FlIcon`, is available. The [implementation plan](docs/planning/implementation-plan.md) says what the library is, how it is built and tested, and in which order; the choices made so far are in [docs/decisions](docs/decisions/README.md).

## Icons

Use `<FlIcon Icon="FlIcons.Search" Label="Search" />` for a standalone image. Omit `Label` for a decorative icon next to text. A host may also construct a `FlIconDefinition` from trusted, noninteractive SVG drawing markup on the Lucide 24 by 24 grid. The markup is rendered as supplied, not sanitized or encoded; never pass user input to the constructor.

## Building

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet build
```

The design system is static HTML and CSS, to be served over HTTP. [CONTRIBUTING.md](CONTRIBUTING.md) says how, and how work is done in this repository.

## What it will be

- **One look.** The FluentCMS design system, themed through tokens: accent, radius, density, typeface, light and dark.
- **Native first.** Built on HTML elements such as `<dialog>`, `popover` and `<select>`, with one small script and no third-party dependency.
- **Every render mode.** Static server-side rendering, interactive server and interactive WebAssembly.
- **Accessible by default.** WCAG 2.2 AA, full keyboard use, right-to-left layout.
- **Small.** Fewer than 40 components, prefixed `Fl`, in the package `FluentCMS.Components`.
- **Designed from scratch.** Its components and API come from the design system and from HTML, not from an earlier library.

## License

[MIT](LICENSE)
