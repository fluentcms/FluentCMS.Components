using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Components.Tests.Foundations;

public sealed class FlButtonTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void DefaultsToAPlainNonSubmittingButton()
    {
        var cut = _context.Render<FlButton>(p => p.AddChildContent("Save"));

        cut.MarkupMatches("""<button type="button" class="fl-button">Save</button>""");
        Assert.Equal(FlButtonVariant.Secondary, cut.Instance.Variant);
        Assert.Equal(FlSize.Medium, cut.Instance.Size);
        Assert.Equal(FlButtonType.Button, cut.Instance.Type);
    }

    [Theory]
    [InlineData(FlButtonVariant.Secondary, FlSize.Medium, false, false, "fl-button")]
    [InlineData(FlButtonVariant.Primary, FlSize.Small, true, true, "fl-button fl-button--primary fl-button--danger fl-button--s fl-button--block")]
    [InlineData(FlButtonVariant.Subtle, FlSize.Large, false, true, "fl-button fl-button--subtle fl-button--l fl-button--block")]
    public void CombinesOnlyRequestedModifiersAndHostClasses(FlButtonVariant variant, FlSize size, bool danger, bool block, string classes)
    {
        var host = new Dictionary<string, object> { ["CLASS"] = "host extra", ["id"] = "save", ["data-extra"] = "42" };
        var cut = _context.Render<FlButton>(p => p.Add(b => b.Variant, variant).Add(b => b.Size, size)
            .Add(b => b.Danger, danger).Add(b => b.Block, block).Add(b => b.AdditionalAttributes, host).AddChildContent("Save"));

        Assert.Equal(classes + " host extra", cut.Find("button#save[data-extra='42']").ClassName);
        Assert.Equal("host extra", host["CLASS"]);
    }

    [Fact]
    public void BusyOverlaysADecorativeSpinnerWithoutReplacingTheActionOrEitherIcon()
    {
        var cut = _context.Render<FlButton>(p => p.Add(b => b.Icon, FlIcons.Save).AddChildContent("Save")
            .Add(b => b.IconEnd, FlIcons.ArrowRight));
        var content = cut.Find("button").InnerHtml;
        Assert.Equal("Save", cut.Find("button").ChildNodes[1].TextContent);
        cut.Render(p => p.Add(b => b.Busy, true));

        var button = cut.Find("button[type='button'][aria-busy='true'][aria-disabled='true']");
        Assert.StartsWith(content, button.InnerHtml, StringComparison.Ordinal);
        Assert.Equal("Save", button.TextContent);
        Assert.False(button.HasAttribute("aria-label"));
        Assert.Equal("Loading", button.GetAttribute("aria-description"));
        Assert.Collection(cut.FindComponents<FlIcon>(),
            leading => Assert.Equal(FlIcons.Save.Content, leading.Instance.Icon.Content),
            trailing => Assert.Equal(FlIcons.ArrowRight.Content, trailing.Instance.Icon.Content),
            spinner => Assert.Equal(FlIcons.LoaderCircle.Content, spinner.Instance.Icon.Content));
        Assert.Equal(3, cut.FindAll("button > svg[aria-hidden='true'][focusable='false']").Count);
        Assert.NotNull(cut.Find("button > .fl-icon.fl-icon--spin.fl-button__spinner:last-child"));

        cut.Render(p => p.Add(b => b.Busy, false));
        Assert.Equal(content, cut.Find("button").InnerHtml);
        Assert.False(cut.Find("button").HasAttribute("aria-description"));
    }

    [Theory]
    [InlineData("Chargement", "Chargement")]
    [InlineData(null, "Loading")]
    public void BusyIconOnlyLinkKeepsItsNameAndGetsAPerKeyLocalizedDescription(string? translation, string expected)
    {
        _context.Services.AddSingleton<IFlTextProvider>(new LoadingProvider(translation));
        var cut = _context.Render<FlButton>(p => p.Add(b => b.Href, "/edit").Add(b => b.Busy, true)
            .Add(b => b.IconOnly, true).Add(b => b.Icon, FlIcons.Pencil).Add(b => b.Label, "Edit")
            .AddUnmatched("aria-LABEL", "other").AddUnmatched("aria-labelledby", "other"));

        var link = cut.Find("a.fl-button--icon[aria-label='Edit']");
        Assert.Equal(expected, link.GetAttribute("aria-description"));
        Assert.False(link.HasAttribute("aria-labelledby"));
        Assert.Empty(link.TextContent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IconOnlyRequiresANonblankLabel(string? label)
    {
        var error = Assert.Throws<InvalidOperationException>(() => _context.Render<FlButton>(p => p
            .Add(b => b.IconOnly, true).Add(b => b.Icon, FlIcons.Pencil).Add(b => b.Label, label)));
        Assert.Contains("Label", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TextButtonRequiresChildContentEvenWithADecorativeIcon(bool hasIcon)
    {
        var error = Assert.Throws<InvalidOperationException>(() => _context.Render<FlButton>(p => p
            .Add(b => b.Icon, hasIcon ? FlIcons.Search : null)));
        Assert.Contains("ChildContent", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Icon")]
    [InlineData("IconEnd")]
    [InlineData("ChildContent")]
    [InlineData("Label")]
    public void RejectsConflictingIconParameters(string parameter)
    {
        var error = Assert.Throws<InvalidOperationException>(() => _context.Render<FlButton>(p =>
        {
            p.Add(b => b.IconOnly, parameter != "Label").Add(b => b.Label, "Edit");
            if (parameter != "Icon")
            {
                p.Add(b => b.Icon, FlIcons.Pencil);
            }
            if (parameter == "IconEnd")
            {
                p.Add(b => b.IconEnd, FlIcons.ArrowRight);
            }
            if (parameter == "ChildContent")
            {
                p.AddChildContent("Other");
            }
        }));
        Assert.Contains(parameter, error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(FlButtonType.Submit, null, "Type")]
    [InlineData(FlButtonType.Reset, null, "Type")]
    [InlineData(FlButtonType.Button, false, "Pressed")]
    public void RejectsButtonOnlySemanticsOnLinks(FlButtonType type, bool? pressed, string parameter)
    {
        var error = Assert.Throws<InvalidOperationException>(() => _context.Render<FlButton>(p => p
            .Add(b => b.Href, "/edit").Add(b => b.Type, type).Add(b => b.Pressed, pressed).AddChildContent("Edit")));
        Assert.Contains(parameter, error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(FlButtonType.Submit, "submit")]
    [InlineData(FlButtonType.Reset, "reset")]
    public async Task BusyNeutralizesNativeActionsAndHandlersThenRestoresThem(FlButtonType type, string htmlType)
    {
        var activation = new Dictionary<string, object>
        {
            ["form"] = "edit", ["formAction"] = "/save", ["formmethod"] = "post", ["formtarget"] = "_blank",
            ["formenctype"] = "text/plain", ["formnovalidate"] = true, ["commandFor"] = "dialog", ["command"] = "show-modal",
            ["popoverTarget"] = "details", ["popovertargetaction"] = "show", ["onKeyDown"] = "void 0",
            ["onKeyUp"] = "void 0", ["onPointerDown"] = "void 0", ["onTouchEnd"] = "void 0", ["onAuxClick"] = "void 0",
        };
        int clicks = 0;
        var cut = _context.Render<FlButton>(p => p.Add(b => b.Type, type).Add(b => b.AdditionalAttributes, activation)
            .Add(b => b.OnClick, () => clicks++).AddChildContent("Save"));
        var button = cut.Find("button");
        Assert.Equal(htmlType, button.GetAttribute("type"));
        Assert.All(activation.Keys, name => Assert.True(button.HasAttribute(name)));
        await button.ClickAsync(new MouseEventArgs());

        cut.Render(p => p.Add(b => b.Busy, true));
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.False(button.HasAttribute("disabled"));
        Assert.False(button.HasAttribute("tabindex"));
        Assert.All(activation.Keys, name => Assert.False(button.HasAttribute(name)));
        await Assert.ThrowsAsync<MissingEventHandlerException>(() => button.ClickAsync(new MouseEventArgs()));
        Assert.Equal(1, clicks);

        cut.Render(p => p.Add(b => b.Busy, false));
        Assert.Equal(htmlType, button.GetAttribute("type"));
        Assert.All(activation.Keys, name => Assert.True(button.HasAttribute(name)));
        await button.ClickAsync(new MouseEventArgs());
        Assert.Equal(2, clicks);
    }

    [Fact]
    public async Task AvailableLinkUsesRealNavigationAndPassesMouseArgs()
    {
        MouseEventArgs? received = null;
        var cut = _context.Render<FlButton>(p => p.Add(b => b.Href, "edit?item=2").AddChildContent("Edit")
            .AddUnmatched("rel", "external").AddUnmatched("target", "_blank")
            .Add(b => b.OnClick, (MouseEventArgs args) => received = args));

        var link = cut.Find("a[href='edit?item=2'][rel='external'][target='_blank']");
        Assert.Equal("Edit", link.TextContent);
        Assert.False(link.HasAttribute("type"));
        Assert.False(link.HasAttribute("role"));
        await link.ClickAsync(new MouseEventArgs { Detail = 2 });
        Assert.Equal(2, received?.Detail);

        cut.Render(p => p.Add(b => b.Busy, true));
        var unavailable = cut.Find("a[role='link'][tabindex='0']");
        Assert.False(unavailable.HasAttribute("href"));
        await Assert.ThrowsAsync<MissingEventHandlerException>(() => unavailable.ClickAsync(new MouseEventArgs()));

        cut.Render(p => p.Add(b => b.Busy, false).Add(b => b.Href, "edit?item=3"));
        var restored = cut.Find("a[href='edit?item=3'][rel='external'][target='_blank']");
        Assert.False(restored.HasAttribute("tabindex"));
        Assert.False(restored.HasAttribute("role"));
        await restored.ClickAsync(new MouseEventArgs { Detail = 1 });
        Assert.Equal(1, received?.Detail);
    }

    [Theory]
    [InlineData(null, "button")]
    [InlineData("/edit", "a")]
    public async Task BusyKeepsHostFocusObserversWithoutRestoringActivation(string? href, string selector)
    {
        var observed = new List<string>();
        Action<FocusEventArgs> observe = args => observed.Add(args.Type!);
        int clicks = 0;
        var cut = _context.Render<FlButton>(p => p.Add(b => b.Href, href).AddChildContent("Edit")
            .Add(b => b.OnClick, () => clicks++).AddUnmatched("onfocus", observe).AddUnmatched("onblur", observe)
            .AddUnmatched("onfocusin", observe).AddUnmatched("onfocusout", observe));
        var control = cut.Find(selector);
        await control.FocusAsync(new FocusEventArgs { Type = "focus" });
        await control.ClickAsync(new MouseEventArgs());

        cut.Render(p => p.Add(b => b.Busy, true));
        await control.BlurAsync(new FocusEventArgs { Type = "blur" });
        await control.FocusOutAsync(new FocusEventArgs { Type = "focusout" });
        await control.FocusAsync(new FocusEventArgs { Type = "focus" });
        await control.FocusInAsync(new FocusEventArgs { Type = "focusin" });
        await Assert.ThrowsAsync<MissingEventHandlerException>(() => control.ClickAsync(new MouseEventArgs()));
        Assert.Equal(["focus", "blur", "focusout", "focus", "focusin"], observed);
        Assert.Equal(1, clicks);
    }

    [Theory]
    [InlineData(false, true, "0", "true")]
    [InlineData(true, false, null, null)]
    [InlineData(true, true, null, null)]
    public async Task UnavailableLinksKeepTheirRoleButNotTheirDestinationOrHandler(bool disabled, bool busy, string? tabIndex, string? ariaBusy)
    {
        var host = new Dictionary<string, object>
        {
            ["HREF"] = "/other", ["Role"] = "button", ["type"] = "submit", ["TABINDEX"] = "5", ["DISABLED"] = true,
            ["Aria-Busy"] = "false", ["aria-disabled"] = "false", ["onClick"] = "void 0", ["target"] = "_blank",
            ["contenteditable"] = "true", ["id"] = "edit",
        };
        var cut = _context.Render<FlButton>(p => p.Add(b => b.Href, "/edit").Add(b => b.Disabled, disabled)
            .Add(b => b.Busy, busy).Add(b => b.OnClick, () => Assert.Fail("Unavailable link invoked OnClick"))
            .Add(b => b.AdditionalAttributes, host).AddChildContent("Edit"));

        var link = cut.Find("a#edit[role='link'][aria-disabled='true']");
        Assert.Equal(tabIndex, link.GetAttribute("tabindex"));
        Assert.Equal(ariaBusy, link.GetAttribute("aria-busy"));
        Assert.Equal(ariaBusy is null, cut.FindAll(".fl-button__spinner").Count == 0);
        foreach (var name in new[] { "href", "type", "disabled", "onclick", "target", "contenteditable" })
        {
            Assert.False(link.HasAttribute(name));
        }
        await Assert.ThrowsAsync<MissingEventHandlerException>(() => link.ClickAsync(new MouseEventArgs()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitlyDisabledNativeButtonUsesOnlyNativeDisabledState(bool busy)
    {
        var cut = _context.Render<FlButton>(p => p.Add(b => b.Disabled, true).Add(b => b.Busy, busy)
            .Add(b => b.OnClick, () => Assert.Fail("Disabled button invoked OnClick")).AddChildContent("Save"));

        cut.MarkupMatches("""<button class="fl-button" type="button" disabled>Save</button>""");
        await Assert.ThrowsAsync<MissingEventHandlerException>(() => cut.Find("button").ClickAsync(new MouseEventArgs()));
    }

    [Fact]
    public void OwnsSemanticsAndRestoresHostDescriptionsAndTabOrderAfterBusy()
    {
        var host = new Dictionary<string, object>
        {
            ["TYPE"] = "reset", ["DISABLED"] = true, ["aria-Pressed"] = "false", ["Role"] = "switch", ["HREF"] = "/wrong",
            ["aria-BUSY"] = "true", ["aria-disabled"] = "true", ["tabIndex"] = "-1", ["ARIA-description"] = "Saves edits",
            ["aria-describedBy"] = "help", ["name"] = "action", ["value"] = "save", ["aria-controls"] = "document",
        };
        var cut = _context.Render<FlButton>(p => p.Add(b => b.Type, FlButtonType.Submit)
            .Add(b => b.AdditionalAttributes, host).AddChildContent("Save"));
        var button = cut.Find("button");
        Assert.Equal("submit", button.GetAttribute("type"));
        foreach (var name in new[] { "disabled", "role", "href", "aria-pressed", "aria-busy", "aria-disabled" })
        {
            Assert.False(button.HasAttribute(name));
        }

        cut.Render(p => p.Add(b => b.Busy, true));
        Assert.Equal("Loading", button.GetAttribute("aria-description"));
        Assert.False(button.HasAttribute("aria-describedby"));
        Assert.False(button.HasAttribute("tabindex"));
        Assert.Equal("document", button.GetAttribute("aria-controls"));

        cut.Render(p => p.Add(b => b.Busy, false));
        Assert.Equal("Saves edits", button.GetAttribute("aria-description"));
        Assert.Equal("help", button.GetAttribute("aria-describedby"));
        Assert.Equal("-1", button.GetAttribute("tabindex"));
        Assert.Equal("action", button.GetAttribute("name"));
        Assert.Equal("save", button.GetAttribute("value"));
    }

    [Fact]
    public async Task PressedTracksTheCallerNotTheNumberOfClicks()
    {
        var pressed = false;
        var cut = _context.Render<FlButton>(p => p.Add(b => b.Pressed, pressed)
            .Add(b => b.OnClick, () => pressed = !pressed).AddChildContent("Bold"));
        var button = cut.Find("button");
        await button.ClickAsync(new MouseEventArgs());
        Assert.True(pressed);
        Assert.Equal("false", button.GetAttribute("aria-pressed"));
        cut.Render(p => p.Add(b => b.Pressed, pressed));
        Assert.Equal("true", button.GetAttribute("aria-pressed"));
        await button.ClickAsync(new MouseEventArgs());
        cut.Render(p => p.Add(b => b.Pressed, pressed));
        Assert.Equal("false", button.GetAttribute("aria-pressed"));

        cut.Render(p => p.Add(b => b.Pressed, null).Add(b => b.Href, "/format").AddChildContent("Format"));
        Assert.Empty(cut.FindAll("button"));
        Assert.False(cut.Find("a[href='/format']").HasAttribute("aria-pressed"));
        Assert.Equal("Format", cut.Find("a").TextContent);
    }

    public void Dispose() => _context.Dispose();

    private sealed class LoadingProvider(string? translation) : IFlTextProvider
    {
        public string? GetText(FlText text) => text == FlText.Loading ? translation : throw new InvalidOperationException();
    }
}
