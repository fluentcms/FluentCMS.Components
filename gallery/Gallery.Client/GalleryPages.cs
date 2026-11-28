namespace Gallery.Client;

public sealed record GalleryPageInfo(string Path, string Title);

// The list of pages in the header. A new page adds its line here; the path is its @page route without the slash.
public static class GalleryPages
{
    public static readonly IReadOnlyList<GalleryPageInfo> All =
    [
        new("", "Start"),
        new("icon", "Icon"),
        new("tokens", "Tokens"),
        new("render-modes", "Render modes"),
    ];
}
