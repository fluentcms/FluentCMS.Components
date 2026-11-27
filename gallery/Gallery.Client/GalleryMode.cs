using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Gallery.Client;

public sealed record GalleryMode(string Segment, string Name, string ShortName)
{
    public static readonly GalleryMode Static = new("static", "Static server-side rendering", "Static");
    public static readonly GalleryMode Server = new("server", "Interactive server", "Server");
    public static readonly GalleryMode WebAssembly = new("wasm", "Interactive WebAssembly", "WebAssembly");

    public static readonly IReadOnlyList<GalleryMode> All = [Static, Server, WebAssembly];

    public static GalleryMode Of(IComponentRenderMode? renderMode) => renderMode switch
    {
        InteractiveServerRenderMode => Server,
        InteractiveWebAssemblyRenderMode => WebAssembly,
        _ => Static,
    };

    // Under the host the base address ends with the segment of a mode: /server/.
    // Any other base means the client was published alone, as a static site.
    public static GalleryMode? FromBase(string baseAddress)
    {
        string path = Uri.TryCreate(baseAddress, UriKind.Absolute, out Uri? uri) ? uri.AbsolutePath : baseAddress;
        string segment = path.TrimEnd('/');
        segment = segment[(segment.LastIndexOf('/') + 1)..];
        return All.FirstOrDefault(mode => mode.Segment == segment);
    }
}
