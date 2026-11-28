using FluentCMS.Components.Browser.Harness;

namespace FluentCMS.Components.Browser.Samples;

public sealed class HarnessGuardTests(GalleryFixture gallery)
{
    // The fragment keeps the options appended by OpenAsync out of the server request.
    private const string LightLtrPage = "tokens?theme=light&dir=ltr#fixed-appearance";

    [Theory]
    [ModeData]
    public async Task OpenRejectsAMissingRoute(RenderMode mode)
    {
        const string path = "missing-harness-test-page";
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using GalleryPage page = await gallery.OpenAsync(mode, path);
        });

        Assert.Contains($"/{mode.Segment()}/{path}", error.Message);
        Assert.Contains("HTTP 404", error.Message);
    }

    [Theory]
    [ModeData(RenderMode.Static)]
    public async Task ScanRejectsALightDocumentForTheDarkRun(RenderMode mode)
    {
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Accessibility.AssertNoViolationsAsync(gallery, mode, LightLtrPage));

        Assert.Contains("theme: expected 'dark', got 'light'", error.Message);
    }

    [Theory]
    [ModeData(RenderMode.Static)]
    public async Task OpenRejectsALeftToRightDocumentWhenRightToLeftWasRequested(RenderMode mode)
    {
        var options = new GalleryOptions { Theme = "light", Direction = "rtl" };
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using GalleryPage page = await gallery.OpenAsync(mode, LightLtrPage, options);
        });

        Assert.Contains("direction: expected 'rtl', got 'ltr'", error.Message);
    }

    [Theory]
    [InlineData(",")]
    [InlineData(" , , ")]
    [InlineData("")]
    [InlineData(" \t")]
    public void ConfiguredListMustSelectAtLeastOneMode(string value)
    {
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => RunSettings.ParseModes(value));

        Assert.Contains(RunSettings.ModesVariable, error.Message);
        Assert.Contains("at least one mode", error.Message);
    }

    [Fact]
    public void AnUnsetListRunsAllModesAndAValidListKeepsItsSelection()
    {
        Assert.Equal(Enum.GetValues<RenderMode>(), RunSettings.ParseModes(null));
        Assert.Equal([RenderMode.Wasm, RenderMode.Static], RunSettings.ParseModes(" wasm, STATIC, "));
    }
}
