# Design

The visual sources the library is built against.

| File | What it is |
| --- | --- |
| [reference/admin.html](reference/admin.html) | The reference design of the FluentCMS admin: the direction selected in November 2026. |
| [system/](system/index.html) | The design system: the tokens, every component with its states, the patterns and the four reference screens. |
| [system/RECIPE.md](system/RECIPE.md) | How a component is drawn and specified. |
| [tools/](tools) | `contrast.mjs` checks the colour tokens against WCAG AA; `icons.mjs` builds the icon set. |

## The reference design

One static page with three screens of the admin (dashboard, content list, entry editor) and sample content, in a light and a dark theme. Open the file in a browser; the pill at the bottom switches the screen and the theme. Links and buttons only move between the three screens.

It sets the direction: a grouped sidebar, a header with global search, bordered inputs, data tables, a right-hand inspector in the editor and a blue accent.

It is a draft. The direction is settled; details may still change as requirements arrive, and no major change is expected.

It is a mockup, not a specification:

- its markup and styles are written for the picture, and none of them is the library's code;
- its class names, sizes and colours are not the tokens; the design system page fixes those;
- it loads Inter and JetBrains Mono from Google Fonts, which the library will not do;
- the icons are drawn after [Lucide](https://lucide.dev), and the library's icon family is still open ([third-party notices](../THIRD-PARTY-NOTICES.md)).

The design system of the [implementation plan](../docs/planning/implementation-plan.md) (section 6, issue C-03) is made from it.

## The design system

Static HTML and CSS under `system/`. The CSS under `system/css/` is the first draft of the library's stylesheet: plain CSS, `fl-` classes, `--fl-` tokens, one file per component. The files under `system/docs/` are the chrome of the pages and never ship.

A browser does not load the icons from a `file://` page. Serve this folder and open the pages over HTTP:

```text
python -m http.server 5600 --bind 127.0.0.1 --directory design
```

Then open `http://localhost:5600/system/index.html`. The switches in the header set the theme, the density and the direction.

```text
node design/tools/contrast.mjs    checks the colour tokens in both themes
node design/tools/icons.mjs       builds the icon set from its pinned version
```
