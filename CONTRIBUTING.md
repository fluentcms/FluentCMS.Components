# Contributing

Thank you for helping. This page says how work is done in this repository. The [implementation plan](docs/planning/implementation-plan.md) says what the library is and what comes next.

## Working rules

These are the rules of FluentCMS, kept here so that one habit serves both repositories.

1. **An issue first.** Every change starts from an issue that says what is wanted and when it is done. A small fix, such as a typo, may skip it.
2. **A branch per issue**, cut from `main` and named `<issue>-<slug>`, for example `29-scaffolding`. Nobody commits to `main` directly.
3. **A pull request into `main`.** Its description says what changed and why, and ends with `Closes #<issue>` so that the issue closes when the pull request merges. A pull request serves one issue.
4. **Short, imperative titles** for commits and pull requests: "Add the button", "Fix focus return in the dialog". No full stop, and no prefix such as `feat:`.
5. **Decision records.** A choice that is costly to reverse, or that a later reader would ask about, is written in [docs/decisions](docs/decisions/README.md) as a short record with a context, a decision and its consequences. A record is not edited to change history: a new record replaces it, and the old one says so.
6. **Tests with behaviour.** A change in behaviour comes with a test that fails without it. The rules for a component are in section 11 of the plan.
7. **No new dependency without saying so.** The library's only package reference is `Microsoft.AspNetCore.Components.Web`. A change that seems to need another package says so in its issue first.

## Building

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). Nothing else is needed to build the library.

```powershell
dotnet build
```

The build treats every warning as an error and runs the trim and AOT analyzers, so it must pass with no warning.

The build also joins the stylesheet files under `src/FluentCMS.Components/Styles/` into `wwwroot/fluentcms-components.css`. That file is generated and not committed: edit the source files, never the joined one.

Packages are restored from nuget.org only (`nuget.config`). Package versions are set once, in `Directory.Packages.props`; a project file names a package without a version. The version of the library itself is not set in the repository: it comes from the release tag.

## The design system

The design system is static HTML under `design/system/`, with no build step. Its pages load the library's stylesheet files from `src/FluentCMS.Components/Styles/`, where the one copy of them lives. Serve the repository root over HTTP, because a browser does not load the icons from a `file://` page:

```powershell
python -m http.server 5600 --bind 127.0.0.1
```

Then open `http://localhost:5600/design/system/index.html`. The switches in the header set the theme, the density and the direction.

[design/README.md](design/README.md) lists what is in the folder, and [design/system/RECIPE.md](design/system/RECIPE.md) says how a component is drawn and written. The colour tokens are checked against WCAG AA with Node:

```powershell
node design/tools/contrast.mjs
```

## The gallery

The gallery is the documentation of the library and the target of its browser tests. `gallery/Gallery` is the host, for static server-side rendering and interactive server mode. `gallery/Gallery.Client` holds the layout and the pages, and is the WebAssembly build.

```powershell
dotnet run --project gallery/Gallery
```

Then open `http://localhost:5610`.

### Addresses

- **The first part of an address is the render mode**: `/static/tokens`, `/server/tokens` and `/wasm/tokens` are the one page that declares `@page "/tokens"`. The host takes that part off as the base of the application (`Program.cs`), and `App.razor` sets the render mode from it. That is the only place where a mode is set: a page never names one, and the library never does. An address with no mode goes to the static one.
- **A link to another mode leaves the base**, so the browser loads a new document. Links inside the gallery are relative, with no slash in front (`tokens`, not `/tokens`), so they stay in the mode.
- **Theme, density and direction are in the query string**: `?theme=dark&density=compact&dir=rtl`, with `auto`, `light` or `dark`, `comfortable` or `compact`, and `ltr` or `rtl`. They become `data-fl-theme`, `data-fl-density` and `dir` on `<html>`. The switches in the header are links that change them, so they work with no script, and every link the gallery writes keeps them. A link to another page inside a page is a `<PageLink>` for that reason.
- **For a test**, `<main>` carries `data-render-mode` (`static`, `server` or `wasm`) and `data-interactive`, which turns to `true` once the page is interactive.

### Writing a page

A page is one file in `gallery/Gallery.Client/Pages` and one line in `GalleryPages.cs`, which is the list of pages in the header.

```razor
@page "/button"

<GalleryPage Title="Button" Level="ComponentLevel.Static">
    <Lede>Starts an action, or goes somewhere when it is a link.</Lede>

    <Section Title="Variants">
        <Demo>
            <FlButton>Secondary</FlButton>
            <FlButton Variant="ButtonVariant.Primary">Primary</FlButton>
        </Demo>
        <Example Code="@Variants" />
    </Section>

    <Section Title="Parameters">
        <ParameterTable>
            <ParameterRow Name="Variant" Type="ButtonVariant" Default="Secondary">The weight of the button.</ParameterRow>
        </ParameterTable>
    </Section>
</GalleryPage>

@code {
    private const string Variants = """
        <FlButton>Secondary</FlButton>
        <FlButton Variant="ButtonVariant.Primary">Primary</FlButton>
        """;
}
```

| Component | For |
| --- | --- |
| `GalleryPage` | The frame: the title, the level of a component, the list of sections |
| `Lede` | The sentence or two under the title |
| `Section` | One section; its title goes in the list of sections |
| `Demo` | A frame around library markup: `Column`, `Canvas`, `Scroll` |
| `Example` | The source of a demo, to be copied |
| `StateMatrix`, `StateRow` | The table of states: variants down, states across |
| `ParameterTable`, `ParameterRow` | The parameters of the component |
| `SpecTable`, `SpecRow` | The element, the ARIA attributes and keys, the classes and the tokens |
| `PageLink` | A link to another page of the gallery |

- A page has no `@rendermode`, no inline `style` attribute and no inline script, so the gallery runs under the content security policy the library is made for.
- The gallery's own classes start with `gallery-` and are in `wwwroot/gallery.css`. The `fl-` prefix is the library's.
- What a page shows must hold in the three modes, in dark, in compact density, in right to left and at 320 pixels wide.

### The static site

```powershell
dotnet publish gallery/Gallery.Client -c Release -o artifacts/gallery
```

`artifacts/gallery/wwwroot` is then a static site that runs the gallery in WebAssembly alone, from `index.html`. The client tells the two cases apart by the base of the address: under the host it ends with a mode, such as `/wasm/`. So the static site must not be served from a folder named `static`, `server` or `wasm`.

## Tests

There are two test projects, both xUnit v3 on the Microsoft Testing Platform. `global.json` switches `dotnet test` to that platform, which the .NET 10 SDK needs for xUnit v3. Both run from the root, and building is part of the run:

```powershell
dotnet test
```

### Component tests

`tests/FluentCMS.Components.Tests` renders components with bUnit and checks their markup, parameters, ARIA attributes and texts. It needs nothing but the SDK.

```powershell
dotnet test --project tests/FluentCMS.Components.Tests
```

### Browser tests

`tests/FluentCMS.Components.Browser` uses Playwright for .NET against the gallery. The fixture starts the built gallery once, on a free port, and stops it when the run ends, whether the tests passed or not. Install the browsers once, after the first build (the script needs PowerShell 7, `pwsh`; Windows PowerShell works with `-ExecutionPolicy Bypass`):

```powershell
pwsh tests/FluentCMS.Components.Browser/bin/Debug/net10.0/playwright.ps1 install chromium
```

Install `firefox` or `webkit` the same way to run those. Then:

```powershell
dotnet test --project tests/FluentCMS.Components.Browser
```

- **One render mode**: `FLUENTCMS_MODES` lists the modes of the run, `static`, `server` and `wasm`, separated by commas. A row for a mode left out is reported as skipped.
- **One browser**: `FLUENTCMS_BROWSER` is `chromium` (the default), `firefox` or `webkit`. `FLUENTCMS_BROWSER_PATH` runs a browser program of your own in place of the one Playwright installed.
- **One test**: `--filter-class` and `--filter-method` take a name, with `*` at either end. A row of a theory shows its mode and page in its name, so `--filter-display-name "*mode: Wasm*"` runs the WebAssembly rows.

```powershell
$env:FLUENTCMS_MODES = "wasm"
$env:FLUENTCMS_BROWSER = "firefox"
dotnet test --project tests/FluentCMS.Components.Browser --filter-class "*AccessibilitySamples"
```

A test is written once for the three modes. The mode comes from `[ModeData]`, and `OpenAsync` opens the page at the address of that mode and, in the interactive modes, waits for `data-interactive="true"`:

```csharp
[Theory]
[ModeData(Pages = ["", "tokens"])]
public Task PageHasNoAxeViolations(RenderMode mode, string page) =>
    Accessibility.AssertNoViolationsAsync(gallery, mode, page);
```

`[ModeData(RenderMode.Static)]` limits a test to the modes it names. The helpers are in `tests/FluentCMS.Components.Browser/Harness`:

| Helper | Checks |
| --- | --- |
| `Accessibility.AssertNoViolationsAsync` | axe-core finds nothing, in light, dark and right to left. A failure names the rule, the target and the help address |
| `page.Keys()` | Drives the page with the keyboard alone: `StartAtTopAsync`, `TabToAsync`, `PressAsync`, `TypeAsync`, `AssertFocusAsync`, `FocusAsync` |
| `Wcag.AssertTargetSizesAsync` | Every target is at least 24 by 24 CSS pixels (2.5.8). The comment on it says which exceptions apply |
| `Wcag.AssertNoHorizontalScrollAsync` | The page does not scroll sideways at 320 pixels (1.4.10) |
| `Wcag.AssertNoMotionAsync` | Nothing moves under reduced motion (2.3.3) |

`tests/FluentCMS.Components.Browser/Samples` holds one test for each helper, which a new component's test is copied from, and `HelperFaultTests`, which proves that each helper fails on a page with the fault it looks for. The faults are put in by script, so the gallery itself holds none.

In the WebKit build that Playwright installs on Windows, links are left out of the Tab order, so a keyboard test that needs a link fails there.

## Markdown

Every Markdown file passes markdownlint with the rules in `.markdownlint-cli2.jsonc`:

```powershell
npx --yes markdownlint-cli2 "**/*.md"
```

## Style

- Clear code over clever code. A comment says why, not what, and only where the reason is not plain.
- No XML documentation comments in the source.
- `.editorconfig` holds the formatting of C#, Razor, CSS, JavaScript, JSON and Markdown. Most editors read it with no setup.
- Documents are written for a maintainer who has not seen the work: full sentences, plain words.

## License

By contributing you agree that your work is under the [MIT license](LICENSE) of the repository.
