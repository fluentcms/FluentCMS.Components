using System.Diagnostics;
using System.Reflection;
using Microsoft.Playwright;

[assembly: AssemblyFixture(typeof(FluentCMS.Components.Browser.Harness.GalleryFixture))]

namespace FluentCMS.Components.Browser.Harness;

// One gallery and one browser for the whole run. The gallery is the built host started as a
// child process on a port the system picks, so nothing here can clash with another run. A test
// class asks for the fixture in its constructor.
public sealed class GalleryFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan InteractiveTimeout = TimeSpan.FromSeconds(30);

    private readonly List<string> _log = [];
    private Process? _process;
    private IPlaywright? _playwright;

    public Uri BaseAddress { get; private set; } = null!;

    public IBrowser Browser { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        try
        {
            BaseAddress = await StartGalleryAsync();
            _playwright = await Playwright.CreateAsync();
            Browser = await RunSettings.BrowserType(_playwright).LaunchAsync(new BrowserTypeLaunchOptions { ExecutablePath = RunSettings.BrowserPath });
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.CloseAsync();
        }

        _playwright?.Dispose();
        StopGallery();
    }

    // Opens one page of the gallery in a context of its own, in the way the options say, and
    // returns when the page is ready: in an interactive mode, when it has become interactive.
    public async Task<GalleryPage> OpenAsync(RenderMode mode, string path, GalleryOptions? options = null)
    {
        options ??= new GalleryOptions();
        IBrowserContext context = await NewContextAsync(options);
        try
        {
            IPage page = await context.NewPageAsync();
            var address = new Uri(BaseAddress, $"{mode.Segment()}/{path}{options.Query}");
            IResponse? response = await page.GotoAsync(address.AbsoluteUri);
            if (response is null || !response.Ok)
            {
                string status = response is null ? "no document response" : $"HTTP {response.Status}";
                throw new InvalidOperationException($"Opening {address} returned {status}.");
            }

            string ready = $"main[data-render-mode='{mode.Segment()}']" + (mode.IsInteractive() ? "[data-interactive='true']" : "");
            await page.WaitForSelectorAsync(ready, new PageWaitForSelectorOptions { Timeout = (float)InteractiveTimeout.TotalMilliseconds });
            if (options.Theme is "light" or "dark")
            {
                await CheckAppearanceAsync(page, "theme", options.Theme, "color-scheme");
            }

            if (options.Direction is not null)
            {
                await CheckAppearanceAsync(page, "direction", options.Direction, "direction");
            }

            return new GalleryPage(context, page, mode, path);
        }
        catch
        {
            await context.CloseAsync();
            throw;
        }
    }

    private static async Task CheckAppearanceAsync(IPage page, string name, string expected, string property)
    {
        string actual = await page.EvaluateAsync<string>("property => getComputedStyle(document.documentElement).getPropertyValue(property)", property);
        if (actual != expected)
        {
            throw new InvalidOperationException($"{page.Url} {name}: expected '{expected}', got '{actual}'.");
        }
    }

    // A page with no address, for markup a test sets itself with SetContentAsync.
    public async Task<GalleryPage> OpenBlankAsync(string html, GalleryOptions? options = null)
    {
        IBrowserContext context = await NewContextAsync(options ?? new GalleryOptions());
        IPage page = await context.NewPageAsync();
        await page.SetContentAsync(html);
        return new GalleryPage(context, page, null, "");
    }

    private Task<IBrowserContext> NewContextAsync(GalleryOptions options) => Browser.NewContextAsync(new BrowserNewContextOptions
    {
        ViewportSize = new ViewportSize { Width = options.Width, Height = options.Height },
        ColorScheme = options.Theme == "dark" ? ColorScheme.Dark : ColorScheme.Light,
        ReducedMotion = options.ReducedMotion ? Microsoft.Playwright.ReducedMotion.Reduce : Microsoft.Playwright.ReducedMotion.NoPreference,
    });

    private async Task<Uri> StartGalleryAsync()
    {
        string targetPath = typeof(GalleryFixture).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "GalleryTargetPath").Value!;
        if (!File.Exists(targetPath))
        {
            throw new FileNotFoundException("The referenced gallery is not built.", targetPath);
        }

        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(targetPath)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        start.ArgumentList.Add(targetPath);
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add("http://127.0.0.1:0");
        // Development, as under dotnet run: the host then finds the static files of the
        // build output through the manifest the build wrote.
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        start.Environment["DOTNET_ENVIRONMENT"] = "Development";
        // The lifetime logger announces the port Kestrel picked, even if the parent logs only warnings.
        start.Environment["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information";
        start.Environment["DOTNET_NOLOGO"] = "1";

        _process = Process.Start(start) ?? throw new InvalidOperationException("The gallery did not start.");
        // Safety net for a test host that ends without disposing the fixture.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => StopGallery();

        var listening = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
        _process.OutputDataReceived += (_, e) => OnLine(e.Data, listening);
        _process.ErrorDataReceived += (_, e) => OnLine(e.Data, listening);
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        _process.EnableRaisingEvents = true;
        _process.Exited += (_, _) => listening.TrySetException(new InvalidOperationException($"The gallery ended before it listened.{Environment.NewLine}{Log()}"));

        try
        {
            return await listening.Task.WaitAsync(StartTimeout);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"The gallery did not listen within {StartTimeout.TotalSeconds:0} seconds.{Environment.NewLine}{Log()}");
        }
    }

    private void OnLine(string? line, TaskCompletionSource<Uri> listening)
    {
        if (line is null)
        {
            return;
        }

        lock (_log)
        {
            _log.Add(line);
        }

        const string marker = "Now listening on: ";
        int at = line.IndexOf(marker, StringComparison.Ordinal);
        if (at >= 0)
        {
            listening.TrySetResult(new Uri(line[(at + marker.Length)..].Trim().TrimEnd('/') + "/"));
        }
    }

    private string Log()
    {
        lock (_log)
        {
            return string.Join(Environment.NewLine, _log.TakeLast(30));
        }
    }

    private void StopGallery()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(10_000);
            }
        }
        catch (InvalidOperationException)
        {
            // Already gone.
        }
    }
}
