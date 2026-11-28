using FluentCMS.Components.Browser.Harness;

namespace FluentCMS.Components.Browser.Samples;

public sealed class AccessibilitySamples(GalleryFixture gallery)
{
    // The recipe for a component page: one row for each page and mode, light, dark and right to left in each.
    [Theory]
    [ModeData(Pages = ["", "tokens", "render-modes"])]
    public Task PageHasNoAxeViolations(RenderMode mode, string page) =>
        Accessibility.AssertNoViolationsAsync(gallery, mode, page);
}
