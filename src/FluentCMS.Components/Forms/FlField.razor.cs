using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FluentCMS.Components;

public sealed partial class FlField : IDisposable
{
    [Parameter, EditorRequired] public string Label { get; set; } = "";
    [Parameter] public string? Help { get; set; }
    [Parameter] public string? Error { get; set; }
    [Parameter] public string? Hint { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Horizontal { get; set; }
    [Parameter] public string? For { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    private static readonly HashSet<string> OwnedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "class", "role", "aria-labelledby", "aria-describedby", "aria-required", "aria-invalid",
    };

    private readonly FlFieldContext _context;
    private EditContext? _subscribed;
    private string _rendered = "";
    private bool _disposed;

    public FlField() => _context = new FlFieldContext(this);

    internal EditContext? EditContext => _subscribed;

    private IEnumerable<KeyValuePair<string, object>>? HostAttributes =>
        AdditionalAttributes?.Where(attribute => !OwnedAttributes.Contains(attribute.Key));

    private string? GroupDescribedBy => _context.IsGroup && _context.Descriptions.Any() ? string.Join(" ", _context.Descriptions) : null;
    private bool IsRadioGroup => _context.GroupRole == "radiogroup";
    private string? GroupRequired => IsRadioGroup && Required ? "true" : null;
    private string? GroupInvalid => IsRadioGroup && _context.ErrorText is not null ? "true" : null;

    private string CssClass
    {
        get
        {
            string classes = Horizontal ? "fl-field fl-field--horizontal" : "fl-field";
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
        if (string.IsNullOrWhiteSpace(Label))
        {
            throw new InvalidOperationException("FlField requires a nonblank Label.");
        }
        if (!ReferenceEquals(_subscribed, CascadedEditContext))
        {
            if (_subscribed is not null)
            {
                _subscribed.OnValidationStateChanged -= OnValidationStateChanged;
            }
            _subscribed = CascadedEditContext;
            if (_subscribed is not null)
            {
                _subscribed.OnValidationStateChanged += OnValidationStateChanged;
            }
        }
    }

    // A control reports in after the field has rendered; render again only when what it reported changed.
    internal void Refresh()
    {
        string snapshot = Snapshot();
        if (!_disposed && snapshot != _rendered)
        {
            _rendered = snapshot;
            StateHasChanged();
        }
    }

    private string Snapshot() => string.Join("|", _context.Controls.Select(control =>
        $"{control.Id}:{control.GroupRole}:{control.MaxLength}:{control.Length}"));

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e) => StateHasChanged();

    void IDisposable.Dispose()
    {
        _disposed = true;
        if (_subscribed is not null)
        {
            _subscribed.OnValidationStateChanged -= OnValidationStateChanged;
        }
    }
}
