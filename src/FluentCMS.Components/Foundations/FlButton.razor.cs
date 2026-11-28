using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FluentCMS.Components;

public sealed partial class FlButton
{
    [Parameter] public FlButtonVariant Variant { get; set; } = FlButtonVariant.Secondary;
    [Parameter] public bool Danger { get; set; }
    [Parameter] public FlSize Size { get; set; } = FlSize.Medium;
    [Parameter] public FlIconDefinition? Icon { get; set; }
    [Parameter] public FlIconDefinition? IconEnd { get; set; }
    [Parameter] public bool IconOnly { get; set; }
    [Parameter] public string? Label { get; set; }
    [Parameter] public string? Href { get; set; }
    [Parameter] public FlButtonType Type { get; set; } = FlButtonType.Button;
    [Parameter] public bool? Pressed { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Block { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = default!;

    private static readonly HashSet<string> OwnedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "class", "type", "href", "role", "disabled", "aria-disabled", "aria-busy", "aria-pressed", "onclick",
    };

    private static readonly HashSet<string> UnavailableAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "tabindex", "contenteditable", "autofocus",
        "form", "formaction", "formenctype", "formmethod", "formnovalidate", "formtarget",
        "command", "commandfor", "popovertarget", "popovertargetaction", "interestfor",
        "target", "download", "ping", "rel", "hreflang", "referrerpolicy",
        "onauxclick", "ondblclick", "onkeydown", "onkeypress", "onkeyup",
        "onmousedown", "onmouseup", "onpointerdown", "onpointerup", "ontouchstart", "ontouchend",
    };

    private bool Unavailable => Disabled || Busy;
    private bool ShowBusy => Busy && !Disabled;
    private string? AriaBusy => ShowBusy ? "true" : null;
    private string? BusyDescription => ShowBusy ? Services.GetText(FlText.Loading) : null;
    private string? AriaPressed => Pressed switch { true => "true", false => "false", null => null };
    private EventCallback<MouseEventArgs> ClickHandler => Unavailable ? default : OnClick;
    private string NativeType => ShowBusy ? "button" : Type switch
    {
        FlButtonType.Submit => "submit",
        FlButtonType.Reset => "reset",
        _ => "button",
    };

    private IEnumerable<KeyValuePair<string, object>>? HostAttributes =>
        AdditionalAttributes?.Where(attribute => IsHostAttribute(attribute.Key));

    // ID references outrank inline ARIA text; reserve both while the component supplies the name or description.
    private bool IsHostAttribute(string name) =>
        !OwnedAttributes.Contains(name) &&
        !(IconOnly && (name.Equals("aria-label", StringComparison.OrdinalIgnoreCase) || name.Equals("aria-labelledby", StringComparison.OrdinalIgnoreCase))) &&
        !(ShowBusy && (name.Equals("aria-description", StringComparison.OrdinalIgnoreCase) || name.Equals("aria-describedby", StringComparison.OrdinalIgnoreCase))) &&
        !(Unavailable && UnavailableAttributes.Contains(name));

    private string CssClass
    {
        get
        {
            var classes = Variant switch
            {
                FlButtonVariant.Primary => "fl-button fl-button--primary",
                FlButtonVariant.Subtle => "fl-button fl-button--subtle",
                _ => "fl-button",
            };
            if (Danger)
            {
                classes += " fl-button--danger";
            }
            classes += Size switch { FlSize.Small => " fl-button--s", FlSize.Large => " fl-button--l", _ => "" };
            if (IconOnly)
            {
                classes += " fl-button--icon";
            }
            if (Block)
            {
                classes += " fl-button--block";
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

    protected override void OnParametersSet()
    {
        if (IconOnly)
        {
            if (string.IsNullOrWhiteSpace(Label))
            {
                throw new InvalidOperationException("IconOnly requires a nonblank Label.");
            }
            if (Icon is null || IconEnd is not null || ChildContent is not null)
            {
                throw new InvalidOperationException("IconOnly requires Icon, without IconEnd or ChildContent.");
            }
        }
        else if (Label is not null)
        {
            throw new InvalidOperationException("Label is for IconOnly; use ChildContent for a visible label.");
        }
        else if (ChildContent is null)
        {
            throw new InvalidOperationException("Supply ChildContent for a visible label, or use IconOnly with Icon and Label.");
        }
        if (Href is not null && (Pressed is not null || Type != FlButtonType.Button))
        {
            throw new InvalidOperationException("A link cannot set Pressed or use a submit/reset Type.");
        }
    }
}
