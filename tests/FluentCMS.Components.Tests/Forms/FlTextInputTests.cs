using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace FluentCMS.Components.Tests.Forms;

public sealed class FlTextInputTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Profile _model = new() { Name = "Quarterly report" };

    [Fact]
    public async Task BindValueWithoutAnEditFormRendersTheNativeBoxAndPostsUnderTheExpressionName()
    {
        string? received = null;
        var cut = _context.Render<FlTextInput>(p => Bind(p, () => _model.Name, value => received = _model.Name = value)
            .AddUnmatched("aria-label", "Project name"));

        cut.MarkupMatches("""
            <span class="fl-input"><input class="fl-input__control" type="text" name="_model.Name" value="Quarterly report" aria-label="Project name"></span>
            """);
        await cut.Find("input").ChangeAsync(new ChangeEventArgs { Value = "Annual report" });
        Assert.Equal("Annual report", received);
        Assert.Equal("Annual report", _model.Name);
    }

    [Fact]
    public void BindValueInsideAnEditFormKeepsTheFrameworkNameAndTheValidationState()
    {
        var editContext = new EditContext(_model);
        var messages = new ValidationMessageStore(editContext);
        var cut = _context.Render<EditForm>(p => p.Add(f => f.EditContext, editContext)
            .Add(f => f.ChildContent, (RenderFragment<EditContext>)(_ => builder =>
            {
                builder.OpenComponent<FlTextInput>(0);
                builder.AddComponentParameter(1, nameof(FlTextInput.Value), _model.Name);
                builder.AddComponentParameter(2, nameof(FlTextInput.ValueChanged), EventCallback.Factory.Create<string?>(this, value => _model.Name = value));
                builder.AddComponentParameter(3, nameof(FlTextInput.ValueExpression), (Expression<Func<string?>>)(() => _model.Name));
                builder.AddAttribute(4, "aria-label", "Project name");
                builder.CloseComponent();
            })));

        var input = cut.Find("input.fl-input__control");
        Assert.Equal("_model.Name", input.GetAttribute("name"));
        Assert.False(input.HasAttribute("aria-invalid"));

        cut.InvokeAsync(() =>
        {
            messages.Add(editContext.Field(nameof(Profile.Name)), "Enter a name.");
            editContext.NotifyValidationStateChanged();
        });
        Assert.Equal("true", cut.Find("input.fl-input__control").GetAttribute("aria-invalid"));

        cut.InvokeAsync(() =>
        {
            messages.Clear();
            editContext.NotifyValidationStateChanged();
        });
        Assert.False(cut.Find("input.fl-input__control").HasAttribute("aria-invalid"));
    }

    [Fact]
    public void AnExplicitOneWayExpressionRendersWithoutAChangeCallback()
    {
        string fixedValue = "ws-1042";
        var cut = _context.Render<FlTextInput>(p => p.Add(i => i.Value, fixedValue)
            .Add(i => i.ValueExpression, () => fixedValue).Add(i => i.ReadOnly, true).AddUnmatched("aria-label", "Workspace ID"));

        var input = cut.Find("input");
        Assert.Equal("ws-1042", input.GetAttribute("value"));
        Assert.Equal("fixedValue", input.GetAttribute("name"));
        Assert.True(input.HasAttribute("readonly"));
    }

    [Fact]
    public void AMissingExpressionIsTheFrameworkError()
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            _context.Render<FlTextInput>(p => p.Add(i => i.Value, "x").AddUnmatched("aria-label", "Name")));
        Assert.Contains("ValueExpression", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AHostNameWinsOverTheExpressionName()
    {
        var cut = _context.Render<FlTextInput>(p => Bind(p, () => _model.Name, _ => { }).AddUnmatched("name", "q")
            .AddUnmatched("aria-label", "Search"));
        Assert.Equal("q", cut.Find("input").GetAttribute("name"));
    }

    [Theory]
    [InlineData(FlTextInputType.Text, "text")]
    [InlineData(FlTextInputType.Email, "email")]
    [InlineData(FlTextInputType.Url, "url")]
    [InlineData(FlTextInputType.Tel, "tel")]
    [InlineData(FlTextInputType.Search, "search")]
    public void TypeChoosesTheNativeType(FlTextInputType type, string html)
    {
        var cut = _context.Render<FlTextInput>(p => Bind(p, () => _model.Name, _ => { }).Add(i => i.Type, type)
            .AddUnmatched("aria-label", "Name"));
        Assert.Equal(html, cut.Find("input").GetAttribute("type"));
    }

    [Fact]
    public void PartsAndModifiersFollowTheDesignOrder()
    {
        var host = new Dictionary<string, object> { ["CLASS"] = "host-box", ["aria-label"] = "Path", ["id"] = "path", ["TYPE"] = "number", ["placeholder"] = "slug" };
        var cut = _context.Render<FlTextInput>(p => Bind(p, () => _model.Name, _ => { }).Add(i => i.Size, FlSize.Small).Add(i => i.Mono, true)
            .Add(i => i.Prefix, "/projects/").Add(i => i.Suffix, ".html").Add(i => i.AdditionalAttributes, host));

        cut.MarkupMatches("""
            <span class="fl-input fl-input--s fl-input--mono host-box">
              <span class="fl-input__affix">/projects/</span>
              <input class="fl-input__control" type="text" id="path" name="_model.Name" value="Quarterly report" aria-label="Path" placeholder="slug">
              <span class="fl-input__affix">.html</span>
            </span>
            """);
        Assert.Equal("host-box", host["CLASS"]);

        cut.Render(p => p.Add(i => i.Size, FlSize.Large).Add(i => i.Mono, false).Add(i => i.Prefix, null).Add(i => i.Suffix, null)
            .Add(i => i.Icon, FlIcons.Search));
        Assert.Equal("fl-input fl-input--l host-box", cut.Find("span").ClassName);
        Assert.NotNull(cut.Find("span.fl-input > svg.fl-icon.fl-input__icon[aria-hidden='true'] + input.fl-input__control"));
    }

    [Fact]
    public void AnIconAndAPrefixCannotShareTheStart()
    {
        var error = Assert.Throws<InvalidOperationException>(() => _context.Render<FlTextInput>(p => Bind(p, () => _model.Name, _ => { })
            .Add(i => i.Icon, FlIcons.Search).Add(i => i.Prefix, "https://").AddUnmatched("aria-label", "Address")));
        Assert.Contains("Prefix", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Effacer", "Afficher le mot de passe", "Effacer", "Afficher le mot de passe")]
    [InlineData(null, null, "Clear", "Show password")]
    public void ActionsAreNamedNonSubmittingButtonsAfterTheInput(string? clear, string? show, string clearName, string showName)
    {
        _context.Services.AddSingleton<IFlTextProvider>(new ActionTexts(clear, show));
        var cut = _context.Render<FlTextInput>(p => Bind(p, () => _model.Name, _ => { }).Add(i => i.Type, FlTextInputType.Password)
            .Add(i => i.Clearable, true).AddUnmatched("aria-label", "Password"));

        var buttons = cut.FindAll("span.fl-input > input.fl-input__control ~ button.fl-input__action");
        Assert.Collection(buttons,
            button =>
            {
                Assert.Equal("button", button.GetAttribute("type"));
                Assert.Equal(clearName, button.GetAttribute("aria-label"));
                Assert.True(button.HasAttribute("data-fl-clear"));
                Assert.False(button.HasAttribute("aria-pressed"));
            },
            button =>
            {
                Assert.Equal("button", button.GetAttribute("type"));
                Assert.Equal(showName, button.GetAttribute("aria-label"));
                Assert.Equal("false", button.GetAttribute("aria-pressed"));
                Assert.True(button.HasAttribute("data-fl-reveal"));
            });
        Assert.All(buttons, button => Assert.NotNull(button.QuerySelector("svg.fl-icon[aria-hidden='true']")));
        Assert.Equal("password", cut.Find("input").GetAttribute("type"));
    }

    [Fact]
    public void ReadOnlyKeepsTheValueAndRevealButDropsClearWhileDisabledDisablesEveryPart()
    {
        var cut = _context.Render<FlTextInput>(p => Bind(p, () => _model.Name, _ => { }).Add(i => i.Type, FlTextInputType.Password)
            .Add(i => i.Clearable, true).Add(i => i.ReadOnly, true).AddUnmatched("aria-label", "Password"));

        var input = cut.Find("input");
        Assert.True(input.HasAttribute("readonly"));
        Assert.False(input.HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-fl-clear]"));
        Assert.False(cut.Find("[data-fl-reveal]").HasAttribute("disabled"));

        cut.Render(p => p.Add(i => i.ReadOnly, false).Add(i => i.Disabled, true));
        input = cut.Find("input");
        Assert.True(input.HasAttribute("disabled"));
        Assert.False(input.HasAttribute("readonly"));
        Assert.Equal(2, cut.FindAll("button.fl-input__action[disabled]").Count);
    }

    [Fact]
    public void SearchUsesThePatternPartsOnTheBox()
    {
        var cut = _context.Render<FlTextInput>(p => Bind(p, () => _model.Name, _ => { }).Add(i => i.Type, FlTextInputType.Search)
            .Add(i => i.Icon, FlIcons.Search).Add(i => i.Clearable, true).AddUnmatched("aria-label", "Search projects")
            .AddUnmatched("placeholder", "Search projects"));

        Assert.Equal("fl-input fl-search-field", cut.Find("span").ClassName);
        Assert.Equal("fl-input__control fl-search-field__control", cut.Find("input").ClassName);
        Assert.Equal("fl-input__action fl-search-field__clear", cut.Find("button").ClassName);
    }

    [Fact]
    public void TheComponentOwnsItsStateAttributes()
    {
        var host = new Dictionary<string, object>
        {
            ["aria-label"] = "Name", ["VALUE"] = "other", ["readonly"] = true, ["Disabled"] = true, ["maxLength"] = "3",
            ["aria-invalid"] = "false", ["required"] = true, ["autocomplete"] = "organization", ["data-extra"] = "1",
        };
        var cut = _context.Render<FlTextInput>(p => Bind(p, () => _model.Name, _ => { }).Add(i => i.AdditionalAttributes, host));

        var input = cut.Find("input");
        Assert.Equal("Quarterly report", input.GetAttribute("value"));
        foreach (string name in new[] { "readonly", "disabled", "maxlength", "aria-invalid" })
        {
            Assert.False(input.HasAttribute(name), name);
        }
        Assert.True(input.HasAttribute("required"));
        Assert.Equal("organization", input.GetAttribute("autocomplete"));
        Assert.Equal("1", input.GetAttribute("data-extra"));

        cut.Render(p => p.Add(i => i.MaxLength, 40).AddUnmatched("aria-invalid", "true"));
        Assert.Equal("40", cut.Find("input").GetAttribute("maxlength"));
    }

    [Fact]
    public void AStandaloneInputTakesItsNameFromTheHostWithoutGeneratedReferences()
    {
        var cut = _context.Render<FlTextInput>(p => Bind(p, () => _model.Name, _ => { }).AddUnmatched("aria-label", "Project name"));
        var input = cut.Find("input");
        Assert.False(input.HasAttribute("id"));
        Assert.False(input.HasAttribute("aria-describedby"));
        Assert.Equal("Project name", input.GetAttribute("aria-label"));
    }

    public void Dispose() => _context.Dispose();

    private static ComponentParameterCollectionBuilder<FlTextInput> Bind(ComponentParameterCollectionBuilder<FlTextInput> p,
        Expression<Func<string?>> expression, Action<string?> changed) =>
        p.Add(i => i.Value, expression.Compile()()).Add(i => i.ValueChanged, changed).Add(i => i.ValueExpression, expression);

    internal sealed class Profile
    {
        public string? Name { get; set; }
    }

    private sealed class ActionTexts(string? clear, string? show) : IFlTextProvider
    {
        public string? GetText(FlText text) => text switch
        {
            FlText.Clear => clear,
            FlText.ShowPassword => show,
            _ => throw new InvalidOperationException(),
        };
    }
}
