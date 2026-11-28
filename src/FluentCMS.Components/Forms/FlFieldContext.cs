using Microsoft.AspNetCore.Components.Forms;

namespace FluentCMS.Components;

// What an input reports to the field around it. A control that is a group of its own, such as
// a radio group, names its role; a field holding several single controls becomes a "group".
internal interface IFlFieldControl
{
    string? Id { get; }
    FieldIdentifier FieldIdentifier { get; }
    int? MaxLength { get; }
    int Length { get; }
    string? GroupRole { get; }
}

// The seam between FlField and the inputs inside it: the field renders the label, error, help
// and meta parts from this state, and each input reads its id, its description and its state.
internal sealed class FlFieldContext(FlField owner)
{
    private readonly List<IFlFieldControl> _controls = [];

    internal IReadOnlyList<IFlFieldControl> Controls => _controls;

    internal string? For => owner.For;

    internal bool Required => owner.Required;

    internal bool IsGroup => _controls.Count > 1 || _controls.Any(control => control.GroupRole is not null);

    internal string? GroupRole => IsGroup ? _controls.Select(control => control.GroupRole).FirstOrDefault(role => role is not null) ?? "group" : null;

    // The id the parts are named after: the control's in a single field, the first one's in a group.
    internal string? BaseId => For ?? _controls.FirstOrDefault()?.Id;

    internal string? LabelId => IsGroup ? Suffix("label") : null;

    internal string? ErrorText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(owner.Error))
            {
                return owner.Error;
            }
            EditContext? edit = owner.EditContext;
            if (edit is null)
            {
                return null;
            }
            string joined = string.Join(" ", _controls.SelectMany(control => edit.GetValidationMessages(control.FieldIdentifier)).Distinct());
            return joined.Length == 0 ? null : joined;
        }
    }

    internal bool HasExplicitError => !string.IsNullOrWhiteSpace(owner.Error);

    internal string? ErrorId => ErrorText is null ? null : Suffix("error");

    internal string? HelpId => string.IsNullOrWhiteSpace(owner.Help) ? null : Suffix("help");

    internal IFlFieldControl? Counted => IsGroup ? null : _controls.FirstOrDefault(control => control.MaxLength is not null);

    internal string? MetaText => Counted is { } counted
        ? $"{counted.Length} / {counted.MaxLength}"
        : string.IsNullOrWhiteSpace(owner.Hint) ? null : owner.Hint;

    internal bool MetaOver => Counted is { } counted && counted.Length > counted.MaxLength;

    internal string? MetaId => MetaText is null ? null : Suffix("meta");

    // The error first, then the help and the meta, as the design orders them.
    internal IEnumerable<string> Descriptions => new[] { ErrorId, HelpId, MetaId }.OfType<string>();

    internal void Attach(IFlFieldControl control)
    {
        if (!_controls.Contains(control))
        {
            _controls.Add(control);
        }
        owner.Refresh();
    }

    internal void Detach(IFlFieldControl control)
    {
        if (_controls.Remove(control))
        {
            owner.Refresh();
        }
    }

    internal void Changed() => owner.Refresh();

    private string? Suffix(string part) => BaseId is { } id ? $"{id}-{part}" : null;
}
