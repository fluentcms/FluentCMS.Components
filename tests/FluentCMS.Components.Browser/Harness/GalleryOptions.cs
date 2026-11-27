namespace FluentCMS.Components.Browser.Harness;

// How a page of the gallery is opened. Theme, Density and Direction are the query string of
// the gallery; the rest is the browser context.
public sealed record GalleryOptions
{
    public string? Theme { get; init; }

    public string? Density { get; init; }

    public string? Direction { get; init; }

    public int Width { get; init; } = 1280;

    public int Height { get; init; } = 800;

    public bool ReducedMotion { get; init; }

    public string Query
    {
        get
        {
            List<string> parts = [];
            if (Theme is not null)
            {
                parts.Add($"theme={Theme}");
            }

            if (Density is not null)
            {
                parts.Add($"density={Density}");
            }

            if (Direction is not null)
            {
                parts.Add($"dir={Direction}");
            }

            return parts.Count == 0 ? "" : "?" + string.Join('&', parts);
        }
    }
}
