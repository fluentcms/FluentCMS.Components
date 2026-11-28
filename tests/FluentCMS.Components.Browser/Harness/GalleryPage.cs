using Microsoft.Playwright;

namespace FluentCMS.Components.Browser.Harness;

// A page of the gallery open in a context of its own. Dispose it to close the context.
public sealed class GalleryPage(IBrowserContext context, IPage page, RenderMode? mode, string path) : IAsyncDisposable
{
    public IPage Page { get; } = page;

    public RenderMode? Mode { get; } = mode;

    public string Path { get; } = path;

    public ValueTask DisposeAsync() => new(context.CloseAsync());
}
