namespace FluentCMS.Components;

// Content is trusted, noninteractive SVG drawing markup. FlIcon renders it without encoding or sanitizing it.
public readonly struct FlIconDefinition(string content)
{
    private readonly string? _content = content ?? throw new ArgumentNullException(nameof(content));

    public string Content => _content ?? string.Empty;
}
