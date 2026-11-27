using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;

namespace FluentCMS.Components.Browser.Harness;

public static class Accessibility
{
    // The three looks every page must pass in: light, dark, and right to left (in light).
    private static readonly (string Name, GalleryOptions Options)[] Schemes =
    [
        ("light", new GalleryOptions { Theme = "light" }),
        ("dark", new GalleryOptions { Theme = "dark" }),
        ("right to left", new GalleryOptions { Theme = "light", Direction = "rtl" }),
    ];

    // Opens the page in each of the three looks and runs axe-core on it. Any violation fails the
    // test, with one block for each rule that names the rule, the targets and the help address.
    public static async Task AssertNoViolationsAsync(GalleryFixture gallery, RenderMode mode, string path)
    {
        List<string> report = [];
        foreach ((string name, GalleryOptions options) in Schemes)
        {
            await using GalleryPage page = await gallery.OpenAsync(mode, path, options);
            List<string> violations = await ViolationsAsync(page.Page);
            if (violations.Count > 0)
            {
                report.Add($"{mode} /{path} in {name}:");
                report.AddRange(violations);
            }
        }

        if (report.Count > 0)
        {
            Assert.Fail(Describe(report));
        }
    }

    // The same check for a page that is already open, in the look it is in.
    public static async Task AssertNoViolationsAsync(IPage page)
    {
        List<string> violations = await ViolationsAsync(page);
        if (violations.Count > 0)
        {
            Assert.Fail(Describe(violations));
        }
    }

    private static string Describe(List<string> lines) =>
        string.Join(Environment.NewLine, ["axe-core found violations.", .. lines]);

    private static async Task<List<string>> ViolationsAsync(IPage page)
    {
        AxeResult result = await page.RunAxe();
        List<string> lines = [];
        foreach (AxeResultItem violation in result.Violations)
        {
            lines.Add($"  {violation.Id} ({violation.Impact}): {violation.Help}");
            lines.Add($"    help: {violation.HelpUrl}");
            lines.AddRange(violation.Nodes.Select(node => $"    target: {string.Join(" ", node.Target)}"));
        }

        return lines;
    }
}
