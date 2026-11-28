using System.Web;
using FluentCMS.Components.Browser.Harness;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace FluentCMS.Components.Browser.Forms;

public sealed class FieldTests(GalleryFixture gallery)
{
    [Theory]
    [ModeData(RenderMode.Static)]
    public async Task NoScriptPostShowsServerValidationThenSavesTheCorrectedValue(RenderMode mode)
    {
        await using IBrowserContext context = await gallery.Browser.NewContextAsync(new() { JavaScriptEnabled = false });
        IPage page = await context.NewPageAsync();
        Uri address = new(gallery.BaseAddress, $"{mode.Segment()}/field?theme=dark&density=compact&dir=rtl");
        Assert.True((await page.GotoAsync(address.AbsoluteUri))?.Ok);
        Assert.False(await page.EvaluateAsync<bool>("() => matchMedia('(scripting: enabled)').matches"));

        ILocator slug = page.GetByRole(AriaRole.Textbox, new() { Name = "Address", Exact = true });
        await slug.FillAsync("Quarterly Report");
        IRequest invalid = await SubmitAsync(page);
        var posted = HttpUtility.ParseQueryString(invalid.PostData!);
        Assert.Equal("POST", invalid.Method);
        Assert.Equal(address.AbsoluteUri, invalid.Url);
        Assert.Equal("field-settings", posted["_handler"]);
        Assert.False(string.IsNullOrEmpty(posted["__RequestVerificationToken"]));
        Assert.Equal("Northwind", posted["Settings.Name"]);
        Assert.Equal("Quarterly Report", posted["Settings.Slug"]);
        Assert.Equal("ws-1042", posted["Settings.Workspace"]);
        Assert.Null(posted["Settings.Plan"]);

        slug = page.GetByRole(AriaRole.Textbox, new() { Name = "Address", Exact = true });
        await Expect(slug).ToHaveValueAsync("Quarterly Report");
        await Expect(slug).ToHaveAttributeAsync("aria-invalid", "true");
        string error = "Use lowercase letters, digits and single hyphens.";
        await Expect(page.Locator("#form p.fl-field__error")).ToHaveTextAsync(error);
        string describedBy = (await slug.GetAttributeAsync("aria-describedby"))!;
        Assert.Equal(error, await page.Locator($"#{describedBy.Split(' ')[0]}").TextContentAsync());
        await Expect(slug).ToHaveAccessibleDescriptionAsync($"{error} Lowercase letters, digits and single hyphens. example.com/…");
        await Expect(page.Locator("#field-saved")).ToHaveCountAsync(0);

        await slug.FillAsync("quarterly-report");
        IRequest valid = await SubmitAsync(page);
        posted = HttpUtility.ParseQueryString(valid.PostData!);
        Assert.Equal("quarterly-report", posted["Settings.Slug"]);
        await Expect(page.Locator("#field-saved")).ToContainTextAsync("address “quarterly-report”, ID “ws-1042”, plan not sent.");
        await Expect(page.Locator("#form p.fl-field__error")).ToHaveCountAsync(0);
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Address", Exact = true })).Not.ToHaveAttributeAsync("aria-invalid", "true");
        Assert.Equal(address.AbsoluteUri, page.Url);
    }

    [Theory]
    [ModeData]
    public async Task EditFormValidationShowsAndClearsInEveryMode(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "field");
        IPage page = sample.Page;
        ILocator slug = page.GetByRole(AriaRole.Textbox, new() { Name = "Address", Exact = true });
        await slug.FillAsync("Not A Slug");
        await page.Locator("#field-submit").ClickAsync();
        slug = page.GetByRole(AriaRole.Textbox, new() { Name = "Address", Exact = true });
        await Expect(slug).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(slug).ToHaveAccessibleDescriptionAsync(new System.Text.RegularExpressions.Regex("^Use lowercase letters, digits and single hyphens\\. "));
        await Expect(page.Locator("#field-saved")).ToHaveCountAsync(0);

        await slug.FillAsync("not-a-slug");
        await page.Keyboard.PressAsync("Tab");
        if (mode.IsInteractive())
        {
            // An interactive EditForm validates the field on change, before the form is submitted.
            await Expect(page.Locator("#form p.fl-field__error")).ToHaveCountAsync(0);
        }
        await page.Locator("#field-submit").PressAsync("Enter");
        await Expect(page.Locator("#field-saved")).ToContainTextAsync("address “not-a-slug”");
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Address", Exact = true })).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(page.Locator("#form p.fl-field__error")).ToHaveCountAsync(0);
    }

    [Theory]
    [ModeData]
    public async Task BindingReachesDotNetOnChangeAndTheCounterFollowsTyping(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "field");
        IPage page = sample.Page;
        ILocator title = page.Locator("#field-title");
        await title.FillAsync("Second draft");
        await page.Keyboard.PressAsync("Tab");
        await Expect(page.Locator("#field-title-value")).ToHaveTextAsync(mode.IsInteractive() ? "Second draft" : "First draft");

        ILocator project = page.GetByRole(AriaRole.Textbox, new() { Name = "Project name", Exact = true });
        string meta = (await project.GetAttributeAsync("aria-describedby"))!.Split(' ')[^1];
        await project.FillAsync("Annual");
        await Expect(page.Locator($"#{meta}")).ToHaveTextAsync(mode.IsInteractive() ? "6 / 60" : "16 / 60");
    }

    [Theory]
    [ModeData]
    public async Task LabelsHelpErrorsAndGroupsAreWiredByName(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "field");
        IPage page = sample.Page;
        ILocator project = page.GetByRole(AriaRole.Textbox, new() { Name = "Project name", Exact = true });
        await Expect(project).ToHaveAccessibleDescriptionAsync("Shown in lists and in the address of the project. 16 / 60");
        await Expect(project).ToHaveAttributeAsync("required", "");
        await Expect(page.Locator("#anatomy .fl-field__required")).ToHaveAttributeAsync("aria-hidden", "true");

        ILocator email = page.GetByRole(AriaRole.Textbox, new() { Name = "Email", Exact = true });
        await Expect(email).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(email).ToHaveAccessibleDescriptionAsync("Enter an address with a domain, such as name@example.com. Used for sign-in and for notifications.");

        ILocator group = page.GetByRole(AriaRole.Group, new() { Name = "Size", Exact = true });
        await Expect(group).ToHaveAccessibleDescriptionAsync("Width and height of the preview.");
        await Expect(group.GetByRole(AriaRole.Textbox, new() { Name = "Width", Exact = true })).ToHaveValueAsync("1200");
        await Expect(group.GetByRole(AriaRole.Textbox, new() { Name = "Height", Exact = true })).ToHaveValueAsync("630");
        await Expect(page.Locator("main fieldset, main legend")).ToHaveCountAsync(0);

        string[] ids = await page.Locator("main [id]").EvaluateAllAsync<string[]>("els => els.map(el => el.id)");
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Theory]
    [ModeData]
    public async Task ReadOnlyIsATabStopAndDisabledIsSkipped(RenderMode mode)
    {
        await using GalleryPage sample = await gallery.OpenAsync(mode, "field");
        IPage page = sample.Page;
        KeyboardDriver keys = page.Keys();
        ILocator workspace = page.GetByRole(AriaRole.Textbox, new() { Name = "Workspace ID", Exact = true });
        await Expect(workspace).ToHaveAttributeAsync("readonly", "");
        await Expect(page.GetByRole(AriaRole.Textbox, new() { Name = "Plan", Exact = true })).ToBeDisabledAsync();
        await page.GetByRole(AriaRole.Textbox, new() { Name = "Address", Exact = true }).FocusAsync();
        await keys.TabAsync();
        await keys.AssertFocusNameAsync("Workspace ID");
        await keys.TypeAsync("x");
        await Expect(workspace).ToHaveValueAsync("ws-1042");
        await keys.TabAsync();
        await keys.AssertFocusAsync("#field-submit");
    }

    private static async Task<IRequest> SubmitAsync(IPage page)
    {
        IResponse response = await page.RunAndWaitForResponseAsync(
            () => page.Locator("#field-submit").PressAsync("Enter"),
            res => res.Request.IsNavigationRequest && res.Request.Frame == page.MainFrame);
        Assert.True(response.Ok);
        return response.Request;
    }
}
