using Microsoft.Playwright;

namespace FluentCMS.Components.Browser.Harness;

// What one run is limited to, from the environment: FLUENTCMS_BROWSER and FLUENTCMS_MODES.
public static class RunSettings
{
    public const string BrowserVariable = "FLUENTCMS_BROWSER";
    public const string ModesVariable = "FLUENTCMS_MODES";
    public const string BrowserPathVariable = "FLUENTCMS_BROWSER_PATH";

    public static string Browser { get; } = (Environment.GetEnvironmentVariable(BrowserVariable) is { Length: > 0 } name ? name : "chromium").ToLowerInvariant();

    // A browser program to run in place of the build that Playwright installed.
    public static string? BrowserPath { get; } = Environment.GetEnvironmentVariable(BrowserPathVariable) is { Length: > 0 } path ? path : null;

    public static IReadOnlyList<RenderMode> Modes { get; } = ParseModes(Environment.GetEnvironmentVariable(ModesVariable));

    public static IBrowserType BrowserType(IPlaywright playwright) => Browser switch
    {
        "chromium" => playwright.Chromium,
        "firefox" => playwright.Firefox,
        "webkit" => playwright.Webkit,
        _ => throw new InvalidOperationException($"{BrowserVariable} is '{Browser}'. Use chromium, firefox or webkit."),
    };

    private static RenderMode[] ParseModes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [.. Enum.GetValues<RenderMode>()];
        }

        return [.. value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(ParseMode)];
    }

    private static RenderMode ParseMode(string name) =>
        Enum.TryParse(name, ignoreCase: true, out RenderMode mode) && Enum.IsDefined(mode)
            ? mode
            : throw new InvalidOperationException($"{ModesVariable} names '{name}'. Use static, server or wasm, separated by commas.");
}
