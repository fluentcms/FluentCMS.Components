namespace Gallery.Client;

// What the layout hands to everything below it: the page in view and the settings of the address.
public sealed record GalleryContext(string Page, GallerySettings Settings)
{
    // An address inside the gallery, relative to the base, that keeps the theme, the density and the direction.
    public string Href(string page, string? fragment = null)
    {
        // An empty href would mean the document in view, not the start page.
        string path = page.Length == 0 ? "./" : page;
        return path + Settings.Query + (fragment is null ? "" : "#" + fragment);
    }
}
