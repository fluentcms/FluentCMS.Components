using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FluentCMS.Components;

public sealed partial class FlTextInput : IFlFieldControl
{
    [Parameter] public FlTextInputType Type { get; set; } = FlTextInputType.Text;
    [Parameter] public FlSize Size { get; set; } = FlSize.Medium;
    [Parameter] public FlIconDefinition? Icon { get; set; }
    [Parameter] public string? Prefix { get; set; }
    [Parameter] public string? Suffix { get; set; }
    [Parameter] public bool Clearable { get; set; }
    [Parameter] public bool Mono { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public int? MaxLength { get; set; }

    [CascadingParameter] private FlFieldContext? Field { get; set; }
    [Inject] private IServiceProvider Services { get; set; } = default!;

    // class goes on the box; the rest of the host's attributes go on the native input.
    private static readonly HashSet<string> OwnedAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "class", "id", "name", "type", "value", "readonly", "disabled", "required", "maxlength", "aria-invalid", "aria-describedby",
        "onchange", "oninput",
    };

    private FlFieldContext? _attached;
    private string? _id;
    private string? _reserved;
    private int? _typed;
    private string? _lastValue;

    string? IFlFieldControl.Id => _id;
    FieldIdentifier IFlFieldControl.FieldIdentifier => FieldIdentifier;
    int IFlFieldControl.Length => _typed ?? CurrentValueAsString?.Length ?? 0;
    string? IFlFieldControl.GroupRole => null;

    private bool InSingleField => Field is { IsGroup: false };

    private string? NameAttribute => string.IsNullOrEmpty(NameAttributeValue) ? null : NameAttributeValue;

    private string NativeType => Type switch
    {
        FlTextInputType.Email => "email",
        FlTextInputType.Url => "url",
        FlTextInputType.Tel => "tel",
        FlTextInputType.Password => "password",
        FlTextInputType.Search => "search",
        _ => "text",
    };

    private string BoxClass
    {
        get
        {
            string classes = "fl-input" + Size switch { FlSize.Small => " fl-input--s", FlSize.Large => " fl-input--l", _ => "" };
            if (Mono)
            {
                classes += " fl-input--mono";
            }
            if (Type == FlTextInputType.Search)
            {
                classes += " fl-search-field";
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

    private string ControlClass
    {
        get
        {
            string classes = Type == FlTextInputType.Search ? "fl-input__control fl-search-field__control" : "fl-input__control";
            // The framework's validation classes stay for hosts that use them; the stylesheet reads aria-invalid.
            return FieldClass is { Length: > 0 } validation ? classes + " " + validation : classes;
        }
    }

    private string ClearClass => Type == FlTextInputType.Search ? "fl-input__action fl-search-field__clear" : "fl-input__action";

    private string FieldClass => EditContext?.FieldCssClass(FieldIdentifier) ?? "";

    private IEnumerable<KeyValuePair<string, object>>? ControlAttributes =>
        AdditionalAttributes?.Where(attribute => !OwnedAttributes.Contains(attribute.Key));

    private bool IsRequired => Field?.Required == true || HostValue("required") is not (null or false);

    private string? AriaInvalid
    {
        get
        {
            bool invalid = EditContext?.GetValidationMessages(FieldIdentifier).Any() == true
                || Field?.HasExplicitError == true
                || (InSingleField && Field!.ErrorText is not null)
                || string.Equals(HostValue("aria-invalid")?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
            return invalid ? "true" : null;
        }
    }

    private string? DescribedBy
    {
        get
        {
            var tokens = new List<string>();
            if (InSingleField)
            {
                tokens.AddRange(Field!.Descriptions);
            }
            if (HostValue("aria-describedby")?.ToString() is { } host)
            {
                foreach (string token in host.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!tokens.Contains(token, StringComparer.Ordinal))
                    {
                        tokens.Add(token);
                    }
                }
            }
            return tokens.Count == 0 ? null : string.Join(" ", tokens);
        }
    }

    // Typing updates the counter of an interactive field before the value is committed on change.
    private EventCallback<ChangeEventArgs> InputHandler => InSingleField && MaxLength is not null
        ? EventCallback.Factory.Create<ChangeEventArgs>(this, OnInput)
        : default;

    private object? HostValue(string name)
    {
        if (AdditionalAttributes is not null)
        {
            foreach (var attribute in AdditionalAttributes)
            {
                if (attribute.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return attribute.Value;
                }
            }
        }
        return null;
    }

    protected override void OnParametersSet()
    {
        if (Icon is not null && Prefix is not null)
        {
            throw new InvalidOperationException("FlTextInput takes an Icon or a Prefix at its start, not both.");
        }

        if (!ReferenceEquals(_attached, Field))
        {
            _attached?.Detach(this);
            ReleaseId();
            _attached = Field;
        }

        string? hostId = HostValue("id")?.ToString();
        if (Field?.For is { } chosen && hostId is not null && hostId != chosen && Field.Controls.Count == 0)
        {
            throw new InvalidOperationException($"The field's For is '{chosen}' but its control has the id '{hostId}'. Set one of them.");
        }

        if (hostId is not null)
        {
            ReleaseId();
            _id = hostId;
        }
        else if (Field is null)
        {
            ReleaseId();
            _id = null;
        }
        else if (_id is null)
        {
            if (Field.For is { } forId && Field.Controls.Count == 0)
            {
                _id = forId;
            }
            else
            {
                _id = _reserved = FlIds.Reserve(Services, FieldIdentifier.FieldName);
            }
        }

        if (!string.Equals(_lastValue, CurrentValueAsString, StringComparison.Ordinal))
        {
            _lastValue = CurrentValueAsString;
            _typed = null;
        }

        if (Field is null)
        {
            return;
        }
        if (Field.Controls.Contains(this))
        {
            Field.Changed();
        }
        else
        {
            Field.Attach(this);
        }
    }

    private void OnInput(ChangeEventArgs args)
    {
        _typed = args.Value?.ToString()?.Length ?? 0;
        Field?.Changed();
    }

    private void ReleaseId()
    {
        if (_reserved is not null)
        {
            FlIds.Release(Services, _reserved);
            _reserved = null;
            _id = null;
        }
    }

    protected override bool TryParseValueFromString(string? value, out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _attached?.Detach(this);
            ReleaseId();
        }
        base.Dispose(disposing);
    }
}
