namespace FluentCMS.Components;

internal sealed class EnglishTextProvider : IFlTextProvider
{
    internal static EnglishTextProvider Instance { get; } = new();

    public string GetText(FlText text) => text switch
    {
        FlText.Close => "Close",
        FlText.Loading => "Loading",
        _ => throw new ArgumentOutOfRangeException(nameof(text), text, "No English text is defined."),
    };
}
