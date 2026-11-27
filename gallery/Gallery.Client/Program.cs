using Gallery.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Under the host, blazor.web.js places the components that App.razor rendered.
// Published alone, wwwroot/index.html is the document and the client is the whole application.
if (GalleryMode.FromBase(builder.HostEnvironment.BaseAddress) is null)
{
    builder.RootComponents.Add<Routes>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}

await builder.Build().RunAsync();
