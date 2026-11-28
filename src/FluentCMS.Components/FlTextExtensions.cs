using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Components;

internal static class FlTextExtensions
{
    [SuppressMessage("Globalization", "CA1305", Justification = "Placeholders in library text follow the UI culture, even when the data culture differs.")]
    internal static string GetText(this IServiceProvider services, FlText text, params object?[] arguments)
    {
        string template = services.GetService<IFlTextProvider>()?.GetText(text) ?? EnglishTextProvider.Instance.GetText(text);
        return arguments.Length == 0 ? template : string.Format(CultureInfo.CurrentUICulture, template, arguments);
    }
}
