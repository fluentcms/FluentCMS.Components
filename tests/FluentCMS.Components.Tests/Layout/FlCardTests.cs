using Bunit;

namespace FluentCMS.Components.Tests.Layout;

public sealed class FlCardTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void TitledSectionNamesItselfFromItsHeadingWhenTheHostGivesAnId()
    {
        var cut = _context.Render<FlCard>(p => p.Add(c => c.Title, "Identity")
            .AddUnmatched("id", "card-identity").AddChildContent("Body"));

        cut.MarkupMatches("""
            <section class="fl-card" id="card-identity" aria-labelledby="card-identity-title">
              <header class="fl-card__header"><h2 class="fl-card__title" id="card-identity-title">Identity</h2></header>
              <div class="fl-card__body">Body</div>
            </section>
            """);
    }

    [Fact]
    public void TitledSectionWithoutAnIdFallsBackToALabelNotAnInventedId()
    {
        var cut = _context.Render<FlCard>(p => p.Add(c => c.Title, "Identity").AddChildContent("Body"));

        Assert.Equal("Identity", cut.Find("section").GetAttribute("aria-label"));
        Assert.False(cut.Find("section").HasAttribute("aria-labelledby"));
        Assert.False(cut.Find("h2").HasAttribute("id"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(6)]
    public void HeadingLevelComesFromTheHost(int level)
    {
        var cut = _context.Render<FlCard>(p => p.Add(c => c.Title, "Members").Add(c => c.HeadingLevel, level).AddChildContent("x"));

        Assert.Equal($"H{level}", cut.Find(".fl-card__title").TagName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void HeadingLevelOutsideOneToSixIsRejected(int level)
    {
        var error = Assert.Throws<InvalidOperationException>(() => _context.Render<FlCard>(p => p
            .Add(c => c.Title, "Members").Add(c => c.HeadingLevel, level).AddChildContent("x")));
        Assert.Contains("HeadingLevel", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ActionsSitBesideTheTitleAndFooterFollowsTheBody()
    {
        var cut = _context.Render<FlCard>(p => p.Add(c => c.Title, "Members").Add(c => c.Actions, "<button type=\"button\">Invite</button>")
            .Add(c => c.Footer, "Showing 3").AddChildContent("List"));

        Assert.Equal("Invite", cut.Find("header > .fl-card__actions > button").TextContent);
        Assert.Equal(["header", "div", "footer"], cut.Find("section").Children.Select(c => c.LocalName));
        Assert.Equal("Showing 3", cut.Find("section > footer.fl-card__footer").TextContent);
    }

    [Fact]
    public void FlushRemovesBodyPaddingByClass()
    {
        var cut = _context.Render<FlCard>(p => p.Add(c => c.Flush, true).AddUnmatched("aria-label", "Files").AddChildContent("<ul></ul>"));

        Assert.NotNull(cut.Find("section > div.fl-card__body.fl-card__body--flush > ul"));
        Assert.Equal("Files", cut.Find("section").GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll("header"));
    }

    [Fact]
    public void HostAttributesAndClassesLandOnTheSectionWithoutMutatingTheDictionary()
    {
        var host = new Dictionary<string, object> { ["CLASS"] = "extra", ["data-x"] = "1", ["aria-label"] = "Open tasks" };
        var cut = _context.Render<FlCard>(p => p.Add(c => c.AdditionalAttributes, host).AddChildContent("24"));

        var section = cut.Find("section[data-x='1']");
        Assert.Equal("fl-card extra", section.ClassName);
        Assert.Equal("Open tasks", section.GetAttribute("aria-label"));
        Assert.Equal(3, host.Count);
    }

    [Fact]
    public void ATitledCardKeepsItsOwnNameOverAHostLabel()
    {
        var cut = _context.Render<FlCard>(p => p.Add(c => c.Title, "Identity").AddUnmatched("id", "a")
            .AddUnmatched("aria-labelledby", "other").AddUnmatched("aria-label", "other").AddChildContent("x"));

        var section = cut.Find("section");
        Assert.Equal("a-title", section.GetAttribute("aria-labelledby"));
        Assert.False(section.HasAttribute("aria-label"));
    }

    [Fact]
    public void ASectionNeedsATitleOrAnAccessibleName()
    {
        var error = Assert.Throws<InvalidOperationException>(() => _context.Render<FlCard>(p => p.AddChildContent("x")));
        Assert.Contains("Title", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ActionsNeedATitleToSitBeside()
    {
        Assert.Throws<InvalidOperationException>(() => _context.Render<FlCard>(p => p
            .AddUnmatched("aria-label", "x").Add(c => c.Actions, "<a href=\"/\">All</a>").AddChildContent("x")));
    }

    [Fact]
    public void LinkedCardIsOneAnchorWhoseBodyHoldsOnlyPhrasingContent()
    {
        var cut = _context.Render<FlCard>(p => p.Add(c => c.Href, "/projects").AddUnmatched("data-x", "1")
            .AddChildContent("<strong>Projects</strong><span>Plan the work.</span>"));

        cut.MarkupMatches("""
            <a class="fl-card fl-card--link" href="/projects" data-x="1"><span class="fl-card__body"><strong>Projects</strong><span>Plan the work.</span></span></a>
            """);
        Assert.Empty(cut.FindAll("section, header, footer, h2, button, a a"));
    }

    [Theory]
    [InlineData("Title")]
    [InlineData("Flush")]
    [InlineData("Actions")]
    [InlineData("Footer")]
    public void LinkedCardRejectsPartsThatCouldNestAnotherControl(string parameter)
    {
        var error = Assert.Throws<InvalidOperationException>(() => _context.Render<FlCard>(p =>
        {
            p.Add(c => c.Href, "/projects").AddChildContent("Projects");
            switch (parameter)
            {
                case "Title": p.Add(c => c.Title, "Projects"); break;
                case "Flush": p.Add(c => c.Flush, true); break;
                case "Actions": p.Add(c => c.Actions, "<button>x</button>"); break;
                default: p.Add(c => c.Footer, "x"); break;
            }
        }));
        Assert.Contains(parameter, error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankTitleIsRejected(string title)
    {
        var error = Assert.Throws<InvalidOperationException>(() => _context.Render<FlCard>(p => p
            .Add(c => c.Title, title).AddUnmatched("aria-label", "Valid name").AddChildContent("x")));
        Assert.Contains("Title", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("aria-label", "")]
    [InlineData("aria-label", "  ")]
    [InlineData("aria-labelledby", "")]
    [InlineData("ARIA-LABELLEDBY", " ")]
    public void BlankAriaNamesDoNotNameASection(string name, string value)
    {
        Assert.Throws<InvalidOperationException>(() => _context.Render<FlCard>(p => p.AddUnmatched(name, value).AddChildContent("x")));
    }

    [Fact]
    public void ANullAriaNameDoesNotNameASection()
    {
        var host = new Dictionary<string, object> { ["aria-label"] = null! };
        Assert.Throws<InvalidOperationException>(() => _context.Render<FlCard>(p => p
            .Add(c => c.AdditionalAttributes, host).AddChildContent("x")));
    }

    [Theory]
    [InlineData("aria-label", "Files")]
    [InlineData("aria-labelledby", "files-heading")]
    public void AValidAriaNameNamesAnUntitledSection(string name, string value)
    {
        var cut = _context.Render<FlCard>(p => p.AddUnmatched(name, value).AddChildContent("x"));
        Assert.Equal(value, cut.Find("section").GetAttribute(name));
    }

    [Fact]
    public void ALinkedCardWithNoContentAndNoNameIsRejected()
    {
        var error = Assert.Throws<InvalidOperationException>(() => _context.Render<FlCard>(p => p.Add(c => c.Href, "/projects")));
        Assert.Contains("ChildContent", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("aria-label", "")]
    [InlineData("aria-label", "  ")]
    [InlineData("aria-labelledby", " ")]
    public void ABlankAriaNameDoesNotNameAnEmptyLinkedCard(string name, string value)
    {
        Assert.Throws<InvalidOperationException>(() => _context.Render<FlCard>(p => p.Add(c => c.Href, "/projects").AddUnmatched(name, value)));
    }

    [Theory]
    [InlineData("aria-label", "Projects")]
    [InlineData("aria-labelledby", "projects-name")]
    public void AnEmptyLinkedCardIsAcceptedWhenTheHostNamesIt(string name, string value)
    {
        var cut = _context.Render<FlCard>(p => p.Add(c => c.Href, "/projects").AddUnmatched(name, value));
        Assert.Equal(value, cut.Find("a.fl-card--link").GetAttribute(name));
    }

    [Fact]
    public void ALinkedCardWithContentNeedsNoExtraName()
    {
        var cut = _context.Render<FlCard>(p => p.Add(c => c.Href, "/projects").AddChildContent("Projects"));
        Assert.Equal("Projects", cut.Find("a").TextContent);
    }

    public void Dispose() => _context.Dispose();
}
