using Microsoft.AspNetCore.Components;

namespace FluentCMS.Components;

public sealed partial class FlIcon
{
    [Parameter, EditorRequired] public FlIconDefinition Icon { get; set; }
    [Parameter] public FlSize Size { get; set; } = FlSize.Medium;
    [Parameter] public string? Label { get; set; }
    [Parameter] public bool Directional { get; set; }
    [Parameter] public bool Spin { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private static readonly HashSet<string> OwnedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "class", "viewBox", "role", "aria-label", "aria-labelledby", "aria-hidden", "focusable", "tabindex", "autofocus", "contenteditable",
        // A focus listener makes an SVG tabbable in Chromium, even with focusable="false".
        "onfocus", "onblur", "onfocusin", "onfocusout",
    };

    private bool HasLabel => !string.IsNullOrWhiteSpace(Label);

    private IEnumerable<KeyValuePair<string, object>>? HostAttributes =>
        AdditionalAttributes?.Where(attribute => !OwnedAttributes.Contains(attribute.Key));

    private string CssClass
    {
        get
        {
            string classes = Size switch
            {
                FlSize.Small => "fl-icon fl-icon--s",
                FlSize.Large => "fl-icon fl-icon--l",
                _ => "fl-icon",
            };
            if (Directional)
            {
                classes += " fl-icon--directional";
            }
            if (Spin)
            {
                classes += " fl-icon--spin";
            }
            if (AdditionalAttributes is not null)
            {
                foreach (var attribute in AdditionalAttributes)
                {
                    if (attribute.Key.Equals("class", StringComparison.OrdinalIgnoreCase) && attribute.Value?.ToString() is { Length: > 0 } extra)
                    {
                        classes += " " + extra;
                    }
                }
            }
            return classes;
        }
    }
}
