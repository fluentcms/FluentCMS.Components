using Gallery.Client;
using Gallery.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFluentCmsComponents();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}

// A render mode is a base of the application: /static/tokens, /server/tokens and /wasm/tokens
// are the one page "/tokens". App.razor reads the base and sets the mode, so a page never
// names one, and a link to another mode leaves the base and loads a new document.
app.Use((context, next) =>
{
    HttpRequest request = context.Request;
    foreach (GalleryMode mode in GalleryMode.All)
    {
        if (request.Path.StartsWithSegments("/" + mode.Segment, out PathString matched, out PathString rest))
        {
            request.PathBase = request.PathBase.Add(matched);
            request.Path = rest;
            return next(context);
        }
    }

    // An address that names no mode goes to the static one.
    context.Response.Redirect($"{request.PathBase}/{GalleryMode.Static.Segment}{request.Path}{request.QueryString}");
    return Task.CompletedTask;
});

// Routing must see the path without its base, so it comes after.
app.UseRouting();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

app.Run();
