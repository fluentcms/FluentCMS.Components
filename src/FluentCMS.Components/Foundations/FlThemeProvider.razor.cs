using Microsoft.AspNetCore.Components;

namespace FluentCMS.Components;

public sealed partial class FlThemeProvider
{
    [Parameter] public FlTheme? Theme { get; set; }
    [Parameter] public FlDensity? Density { get; set; }
    [Parameter] public FlDirection? Direction { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private static readonly HashSet<string> OwnedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "data-fl-theme", "data-fl-density", "dir",
    };

    private IEnumerable<KeyValuePair<string, object>>? HostAttributes =>
        AdditionalAttributes?.Where(attribute => !OwnedAttributes.Contains(attribute.Key));

    private string? ThemeAttribute => Theme switch
    {
        FlTheme.Light => "light",
        FlTheme.Dark => "dark",
        _ => null,
    };

    private string? DensityAttribute => Density switch
    {
        FlDensity.Comfortable => "comfortable",
        FlDensity.Compact => "compact",
        _ => null,
    };

    private string? DirectionAttribute => Direction switch
    {
        FlDirection.Ltr => "ltr",
        FlDirection.Rtl => "rtl",
        _ => null,
    };
}
