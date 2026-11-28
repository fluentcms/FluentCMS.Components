using Microsoft.AspNetCore.Components;

namespace FluentCMS.Components;

public sealed partial class FlCard
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public int HeadingLevel { get; set; } = 2;
    [Parameter] public string? Href { get; set; }
    [Parameter] public bool Flush { get; set; }
    [Parameter] public RenderFragment? Actions { get; set; }
    [Parameter] public RenderFragment? Footer { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    private static readonly HashSet<string> LinkOwned = new(StringComparer.OrdinalIgnoreCase) { "class", "href" };
    private static readonly HashSet<string> SectionOwned = new(StringComparer.OrdinalIgnoreCase) { "class" };
    private static readonly HashSet<string> NameAttributes = new(StringComparer.OrdinalIgnoreCase) { "aria-label", "aria-labelledby" };

    private string? HostId => AdditionalAttributes?
        .FirstOrDefault(a => a.Key.Equals("id", StringComparison.OrdinalIgnoreCase)).Value?.ToString() is { Length: > 0 } id ? id : null;

    // The heading id is derived from the host's id, so it cannot change when the card becomes interactive.
    private string? TitleId => HostId is { } id ? id + "-title" : null;
    private string? LabelledBy => Title is not null && TitleId is not null ? TitleId : null;
    private string? Label => Title is not null && TitleId is null ? Title : null;
    // A name only counts when it has text; the component cannot tell whether the id of aria-labelledby exists.
    private bool HasHostName => AdditionalAttributes?.Any(a =>
        NameAttributes.Contains(a.Key) && !string.IsNullOrWhiteSpace(a.Value?.ToString())) == true;

    private string BodyClass => Flush ? "fl-card__body fl-card__body--flush" : "fl-card__body";

    private IEnumerable<KeyValuePair<string, object>>? HostAttributes => AdditionalAttributes?.Where(a =>
        !(Href is not null ? LinkOwned : SectionOwned).Contains(a.Key) && !(Href is null && Title is not null && NameAttributes.Contains(a.Key)));

    private string CssClass
    {
        get
        {
            string classes = Href is not null ? "fl-card fl-card--link" : "fl-card";
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
        if (Href is not null)
        {
            string? part = Title is not null ? nameof(Title) : Actions is not null ? nameof(Actions)
                : Footer is not null ? nameof(Footer) : Flush ? nameof(Flush) : null;
            if (part is not null)
            {
                throw new InvalidOperationException($"A linked card is one link and cannot nest other controls; remove {part}.");
            }
            if (ChildContent is null && !HasHostName)
            {
                throw new InvalidOperationException("A linked card needs ChildContent, or an aria-label or aria-labelledby with text, to have a name.");
            }
            return;
        }

        if (Title is not null && string.IsNullOrWhiteSpace(Title))
        {
            throw new InvalidOperationException("Title must contain text; omit it for a card with no header.");
        }
        if (HeadingLevel is < 1 or > 6)
        {
            throw new InvalidOperationException("HeadingLevel must be from 1 to 6.");
        }
        if (Title is null)
        {
            if (Actions is not null)
            {
                throw new InvalidOperationException("Actions sit beside the Title; supply a Title.");
            }
            if (!HasHostName)
            {
                throw new InvalidOperationException("A card section needs a Title, or an aria-label or aria-labelledby.");
            }
        }
    }
}
