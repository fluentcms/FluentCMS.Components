using Bunit;

namespace FluentCMS.Components.Tests;

public sealed class SampleComponentTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void RendersAButtonWithItsLabel()
    {
        IRenderedComponent<SampleComponent> cut = _context.Render<SampleComponent>(parameters => parameters
            .Add(component => component.Label, "Save"));

        cut.MarkupMatches("""<button type="button" class="fl-sample">Save</button>""");
    }

    [Fact]
    public void FollowsAChangedParameter()
    {
        IRenderedComponent<SampleComponent> cut = _context.Render<SampleComponent>(parameters => parameters
            .Add(component => component.Label, "Save"));

        cut.Render(parameters => parameters.Add(component => component.Label, "Saved"));

        Assert.Equal("Saved", cut.Find("button").TextContent);
    }

    public void Dispose() => _context.Dispose();
}
