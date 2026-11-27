namespace FluentCMS.Components.Browser.Harness;

public enum RenderMode
{
    Static,
    Server,
    Wasm,
}

public static class RenderModeExtensions
{
    // The first part of a gallery address, and the value of data-render-mode on <main>.
    public static string Segment(this RenderMode mode) => mode switch
    {
        RenderMode.Static => "static",
        RenderMode.Server => "server",
        RenderMode.Wasm => "wasm",
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    public static bool IsInteractive(this RenderMode mode) => mode != RenderMode.Static;
}
