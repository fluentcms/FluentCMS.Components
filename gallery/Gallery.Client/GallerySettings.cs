using System.Web;

namespace Gallery.Client;

public sealed record GallerySettings(string Theme, string Density, string Direction)
{
    public static readonly IReadOnlyList<string> Themes = ["auto", "light", "dark"];
    public static readonly IReadOnlyList<string> Densities = ["comfortable", "compact"];
    public static readonly IReadOnlyList<string> Directions = ["ltr", "rtl"];

    public static GallerySettings FromUri(string uri)
    {
        var query = HttpUtility.ParseQueryString(new Uri(uri).Query);
        return new(
            Pick(Themes, query["theme"]),
            Pick(Densities, query["density"]),
            Pick(Directions, query["dir"]));
    }

    // The theme attribute is left out for "auto", so that the page follows the system.
    public string? ThemeAttribute => Theme == Themes[0] ? null : Theme;

    // Only what differs from the default is written, so a plain address stays plain.
    public string Query
    {
        get
        {
            List<string> parts = [];
            if (Theme != Themes[0])
            {
                parts.Add($"theme={Theme}");
            }

            if (Density != Densities[0])
            {
                parts.Add($"density={Density}");
            }

            if (Direction != Directions[0])
            {
                parts.Add($"dir={Direction}");
            }

            return parts.Count == 0 ? "" : "?" + string.Join('&', parts);
        }
    }

    private static string Pick(IReadOnlyList<string> allowed, string? value) =>
        value is not null && allowed.Contains(value) ? value : allowed[0];
}
