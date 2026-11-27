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

Packages are restored from nuget.org only (`nuget.config`). Package versions are set once, in `Directory.Packages.props`; a project file names a package without a version. The version of the library itself is not set in the repository: it comes from the release tag.

## The design system

The design system is static HTML and CSS under `design/system/`, with no build step. Serve the `design` folder over HTTP, because a browser does not load the icons from a `file://` page:

```powershell
python -m http.server 5600 --bind 127.0.0.1 --directory design
```

Then open `http://localhost:5600/system/index.html`. The switches in the header set the theme, the density and the direction.

[design/README.md](design/README.md) lists what is in the folder, and [design/system/RECIPE.md](design/system/RECIPE.md) says how a component is drawn and written. The colour tokens are checked against WCAG AA with Node:

```powershell
node design/tools/contrast.mjs
```

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
