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
7. **No new dependency without saying so.** The library's only runtime package dependency is `Microsoft.AspNetCore.Components.Web`. Its public API analyzer runs at build time and is not shipped. A change that needs another package says so in its issue first.

## Building

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). Nothing else is needed to build the library.

```powershell
dotnet build
```

The build treats every warning as an error and runs the trim and AOT analyzers, so it must pass with no warning.

The build also joins the stylesheet files under `src/FluentCMS.Components/Styles/` into `wwwroot/fluentcms-components.css`. That file is generated and not committed: edit the source files, never the joined one.

Packages are restored from nuget.org only (`nuget.config`). The library requires `Microsoft.AspNetCore.Components.Web` 10.0.0 or later so it does not force a host onto a newer patch. The gallery and tests use the current patch recorded in `Directory.Packages.props`. The library's own release version comes from the tag.

## Public API

When adding a public type or member to the library, add its signature to `src/FluentCMS.Components/PublicAPI.Unshipped.txt`, in sorted order. Include nullable annotations: `string` and `string?` are different contracts. `dotnet build` reports `RS0016` as an error if a public symbol is missing. Keep `PublicAPI.Shipped.txt` for released signatures; do not edit it to make a new member pass.

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

`FlIcons` and the gallery's icon list are generated offline from the committed Lucide sprite. Run `node design/tools/generate-icons.mjs` after a sprite change, then `node --test design/tools/generate-icons.test.mjs` to check the naming rules and both generated files. The generator converts kebab-case to PascalCase (so a C# keyword such as `class` becomes `Class`) and prefixes a leading digit with `Icon`.

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

## The script

`src/FluentCMS.Components/wwwroot/FluentCMS.Components.lib.module.js` is the package's JavaScript initializer. A Blazor host does not add a script tag for it. A plain HTML page can import `_content/FluentCMS.Components/FluentCMS.Components.lib.module.js` as an external module. The initializer enhances existing HTML; it does not select a render mode or restore focus across prerendering.

Start with the native element and its attributes. If a browser already performs the interaction, do not implement it again. Where an event suffices, listen on the document so that replacement markup and enhanced navigation do not require new listeners. Use `data-fl-*` for a library state that HTML cannot express. Prefer a bubbling native `input` or `change` event over a .NET call per interaction.

The first behaviours keep a popover trigger's `aria-expanded` in sync, set a checkbox's `indeterminate` property from `data-fl-indeterminate`, and operate the two step buttons of a number input. In the interactive number example, `data-fl-value` mirrors the bound model so a render can tell the initializer to recheck the limits. The live input remains the source of the number. If plain JavaScript assigns `.value` directly, dispatch a bubbling `input` event; a property assignment by itself raises no event and does not change the HTML attribute.

If the native event does not carry the data a component needs, register a distinct custom event name with `blazor.registerCustomEventType`, and declare its `on…` name and argument type with `[EventHandler]` on an `EventHandlers` class in scope for the Razor page. The gallery's `Gallery.Client.lib.module.js` and `EventHandlers.cs` show `flsamplechange` mapped to native `change` and `ChangeEventArgs`. This example is not part of the library package.

To add a behaviour, include a real specimen and a browser test of the interaction, both on direct entry and after navigation. Run it in static SSR, interactive server and WebAssembly. For an interactive binding, assert the value received by .NET, not just the value in the input. Verify custom event arguments in a trimmed publish as well.

For the standalone check, publish and serve the gallery as described above under **The static site**. Set `FLUENTCMS_PUBLISHED_URL` to the served site's address, then run `dotnet test --project tests/FluentCMS.Components.Browser --explicit only --filter-method "*PublishedStandaloneRegistersTypedEvent" --minimum-expected-tests 1`. This test requires the published site; a normal browser run reports it as explicitly skipped.

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

- **One render mode**: leave `FLUENTCMS_MODES` unset to run all three, or list the modes to run (`static`, `server`, `wasm`), separated by commas. A configured list must select at least one mode. Modes deliberately left out are reported as skipped.
- **One browser**: `FLUENTCMS_BROWSER` is `chromium` (the default), `firefox` or `webkit`. `FLUENTCMS_BROWSER_PATH` runs a browser program of your own in place of the one Playwright installed.
- **One test**: `--filter-class` and `--filter-method` take a name, with `*` at either end. A row of a theory shows its mode and page in its name, so `--filter-display-name "*mode: Wasm*"` runs the WebAssembly rows.

```powershell
$env:FLUENTCMS_MODES = "wasm"
$env:FLUENTCMS_BROWSER = "firefox"
dotnet test --project tests/FluentCMS.Components.Browser --filter-class "*AccessibilitySamples"
```

A test is written once for the three modes. The mode comes from `[ModeData]`. `OpenAsync` requires a successful document response and waits for the requested mode; in an interactive mode it also waits for `data-interactive="true"`. Before an axe scan starts, the fixture checks that the document's computed colour scheme and direction match the request. A 404 page or a second LTR scan in place of RTL fails the test.

For example:

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
| `page.Keys()` | Drives the page with the keyboard. `AssertFocusNameAsync` checks the focused element's accessible name with Playwright; `TabToAndAssertPaintChangeAsync` checks a known point on its focus indicator |
| `Wcag.AssertTargetSizesAsync` | Every operable target is at least 24 by 24 CSS pixels in both densities. This is the [project rule](design/system/RECIPE.md), not a check that permits smaller targets if they are spaced apart |
| `Wcag.AssertNoHorizontalScrollAsync` | The page does not scroll sideways at 320 pixels (1.4.10) |
| `Wcag.AssertNoMotionAsync` | Watches for movement under reduced motion before and during the supplied interaction. A colour change alone is not movement |

For a focus ring drawn on a wrapper, pass the wrapper as `indicator` and choose a point on the ring: the offset is measured from the left edge, halfway down. This checks a visible change at that point, not focus contrast across the whole control. For motion, pass the action itself, for example `Wcag.AssertNoMotionAsync(page, () => button.HoverAsync())`. Checking only after the action can miss a transition that has already finished.

`tests/FluentCMS.Components.Browser/Samples` holds working examples and regressions that put each helper in front of the fault it should catch. The gallery itself holds no deliberate fault. To check gallery startup with a non-default build path, a production parent environment, and quiet logging, run the separate regression from the repository root:

```powershell
node tests/FluentCMS.Components.Browser/test-startup.mjs
```

In the WebKit build that Playwright installs on Windows, links are left out of the Tab order, so a keyboard test that needs a link fails there.

## Continuous integration

Every pull request and push to `main` runs four checks in parallel on Ubuntu: **Build, pack and size report**, **Component tests (bUnit)**, **Browser tests (Chromium)**, and **Design system**. The browser check runs static SSR, interactive server and WebAssembly. The nightly workflow runs the same suite in Firefox and WebKit; a Linux WebKit run is not a Safari run. Both workflows can also be started from the Actions tab.

To repeat the PR checks locally, install the Chromium engine as described above, then run from the repository root:

```powershell
dotnet build -c Release -warnaserror
dotnet pack src/FluentCMS.Components -c Release --no-build -o artifacts/package
dotnet test --project tests/FluentCMS.Components.Tests
dotnet test --project tests/FluentCMS.Components.Browser
node design/tools/contrast.mjs
npx --yes markdownlint-cli2@0.24.2 "**/*.md" "#**/bin" "#**/obj"
```

The build summary reports gzip bytes for each shipping asset, not the size of the whole NuGet archive. To reproduce it, pass the packed `.nupkg` to `python .github/scripts/package_size.py`. An absent initializer is reported as absent, not as zero bytes. Size is measured now; budgets are set in #48.

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
