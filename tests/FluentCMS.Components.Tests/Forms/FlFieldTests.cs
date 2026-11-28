using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace FluentCMS.Components.Tests.Forms;

public sealed class FlFieldTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly Project _model = new() { Name = "Quarterly report", Email = "ada@example" };

    [Fact]
    public void WiresTheLabelHelpAndCounterToTheControlInTheDesignOrder()
    {
        var cut = _context.Render<FlField>(p => p.Add(f => f.Label, "Project name").Add(f => f.Required, true)
            .Add(f => f.Help, "Shown in lists and in the address of the project.")
            .AddChildContent<FlTextInput>(i => Bind(i, () => _model.Name).Add(t => t.MaxLength, 60)));

        cut.MarkupMatches("""
            <div class="fl-field">
              <label class="fl-field__label" for="fl-name">Project name <span class="fl-field__required" aria-hidden="true">*</span></label>
              <span class="fl-input"><input class="fl-input__control" type="text" id="fl-name" name="_model.Name" value="Quarterly report"
                maxlength="60" required aria-describedby="fl-name-help fl-name-meta"></span>
              <p class="fl-field__help" id="fl-name-help">Shown in lists and in the address of the project.</p>
              <span class="fl-field__meta" id="fl-name-meta">16 / 60</span>
            </div>
            """);
    }

    [Fact]
    public void AnErrorOutsideAFormComesFirstAndKeepsHostReferencesOnce()
    {
        var cut = _context.Render<FlField>(p => p.Add(f => f.Label, "Email").Add(f => f.Help, "Used for sign-in.")
            .Add(f => f.Error, "Enter an address with a domain.")
            .AddChildContent<FlTextInput>(i => Bind(i, () => _model.Email).Add(t => t.Type, FlTextInputType.Email)
                .AddUnmatched("aria-describedby", "policy fl-email-help  policy")));

        var input = cut.Find("input");
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal("fl-email-error fl-email-help policy", input.GetAttribute("aria-describedby"));
        var error = cut.Find("p.fl-field__error#fl-email-error");
        Assert.Equal("Enter an address with a domain.", error.TextContent);
        Assert.NotNull(error.QuerySelector("svg.fl-icon.fl-icon--s[aria-hidden='true']:first-child"));
        Assert.Collection(cut.Find("div.fl-field").Children,
            label => Assert.Equal("LABEL", label.TagName),
            box => Assert.Equal("fl-input", box.ClassName),
            message => Assert.Equal("fl-field__error", message.ClassName),
            help => Assert.Equal("fl-field__help", help.ClassName));

        cut.Render(p => p.Add(f => f.Error, null).Add(f => f.Help, null));
        input = cut.Find("input");
        Assert.False(input.HasAttribute("aria-invalid"));
        Assert.Equal("policy fl-email-help", input.GetAttribute("aria-describedby"));
        Assert.Empty(cut.FindAll(".fl-field__error, .fl-field__help"));
    }

    [Fact]
    public void EditContextMessagesShowAndClearAfterRerender()
    {
        var editContext = new EditContext(_model);
        var messages = new ValidationMessageStore(editContext);
        var cut = _context.Render<EditForm>(p => p.Add(f => f.EditContext, editContext)
            .Add(f => f.ChildContent, (RenderFragment<EditContext>)(_ => builder => Field(builder, "Email", "Used for sign-in.", () => _model.Email))));

        Assert.Empty(cut.FindAll(".fl-field__error"));
        Assert.Equal("fl-email-help", cut.Find("input").GetAttribute("aria-describedby"));

        cut.InvokeAsync(() =>
        {
            messages.Add(editContext.Field(nameof(Project.Email)), "Enter a full email address.");
            editContext.NotifyValidationStateChanged();
        });
        Assert.Equal("Enter a full email address.", cut.Find("#fl-email-error").TextContent);
        Assert.Equal("fl-email-error fl-email-help", cut.Find("input").GetAttribute("aria-describedby"));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));

        cut.InvokeAsync(() =>
        {
            messages.Clear();
            editContext.NotifyValidationStateChanged();
        });
        Assert.Empty(cut.FindAll(".fl-field__error"));
        Assert.Equal("fl-email-help", cut.Find("input").GetAttribute("aria-describedby"));
        Assert.False(cut.Find("input").HasAttribute("aria-invalid"));
    }

    [Fact]
    public async Task TheCounterFollowsTypingAndMarksALongValue()
    {
        _model.Name = "A summary that runs past the limit of forty characters";
        var cut = _context.Render<FlField>(p => p.Add(f => f.Label, "Summary")
            .AddChildContent<FlTextInput>(i => Bind(i, () => _model.Name).Add(t => t.MaxLength, 40)));

        Assert.Equal("fl-field__meta fl-field__meta--over", cut.Find("#fl-name-meta").ClassName);
        Assert.Equal("54 / 40", cut.Find("#fl-name-meta").TextContent);

        await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "Short" });
        Assert.Equal("5 / 40", cut.Find("#fl-name-meta").TextContent);
        Assert.Equal("fl-field__meta", cut.Find("#fl-name-meta").ClassName);
    }

    [Fact]
    public void AHintFillsTheMetaSlotWhenThereIsNoCounter()
    {
        var cut = _context.Render<FlField>(p => p.Add(f => f.Label, "Company").Add(f => f.Hint, "Optional")
            .AddChildContent<FlTextInput>(i => Bind(i, () => _model.Company)));

        Assert.Equal("Optional", cut.Find("span.fl-field__meta#fl-company-meta").TextContent);
        Assert.Equal("fl-company-meta", cut.Find("input").GetAttribute("aria-describedby"));
    }

    [Fact]
    public void SiblingIdsAreUniqueAndARenewedScopeRepeatsThem()
    {
        Project[] items = [new() { Name = "One" }, new() { Name = "Two" }];
        string[] Render(BunitContext context)
        {
            var cut = context.Render<Siblings>(p => p.Add(s => s.Items, items));
            cut.Render(p => p.Add(s => s.Items, items));
            return [.. cut.FindAll("input").Select(input => input.Id!)];
        }

        string[] first = Render(_context);
        Assert.Equal(["fl-name", "fl-name-2"], first);
        using var renewed = new BunitContext();
        Assert.Equal(first, Render(renewed));
    }

    [Fact]
    public void AnIdFromTheHostOrForWinsOverTheGeneratedOne()
    {
        var cut = _context.Render<FlField>(p => p.Add(f => f.Label, "Name").Add(f => f.Help, "Help")
            .AddChildContent<FlTextInput>(i => Bind(i, () => _model.Name).AddUnmatched("id", "project-name")));
        Assert.Equal("project-name", cut.Find("label").GetAttribute("for"));
        Assert.Equal("project-name-help", cut.Find("input#project-name").GetAttribute("aria-describedby"));

        var byFor = _context.Render<FlField>(p => p.Add(f => f.Label, "Name").Add(f => f.For, "chosen")
            .AddChildContent<FlTextInput>(i => Bind(i, () => _model.Name)));
        Assert.Equal("chosen", byFor.Find("label").GetAttribute("for"));
        Assert.NotNull(byFor.Find("input#chosen"));

        var error = Assert.Throws<InvalidOperationException>(() => _context.Render<FlField>(p => p.Add(f => f.Label, "Name").Add(f => f.For, "chosen")
            .AddChildContent<FlTextInput>(i => Bind(i, () => _model.Name).AddUnmatched("id", "other"))));
        Assert.Contains("For", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AFieldAroundSeveralControlsIsANamedGroupWithoutAFieldset()
    {
        var cut = _context.Render<FlField>(p => p.Add(f => f.Label, "Size").Add(f => f.Help, "Width and height of the preview.")
            .Add(f => f.ChildContent, (RenderFragment)(builder =>
            {
                Input(builder, 0, () => _model.Width, "Width");
                Input(builder, 10, () => _model.Height, "Height");
            })));

        var field = cut.Find("div.fl-field[role='group']");
        Assert.Equal("fl-width-label", field.GetAttribute("aria-labelledby"));
        Assert.Equal("fl-width-help", field.GetAttribute("aria-describedby"));
        var label = cut.Find("div.fl-field__label#fl-width-label");
        Assert.Equal("Size", label.TextContent);
        Assert.Empty(cut.FindAll("label, fieldset, legend"));
        Assert.All(cut.FindAll("input"), input => Assert.False(input.HasAttribute("aria-describedby")));
        Assert.Equal(["Width", "Height"], cut.FindAll("input").Select(input => input.GetAttribute("aria-label")));
    }

    [Fact]
    public void HorizontalAndHostAttributesGoOnTheField()
    {
        var cut = _context.Render<FlField>(p => p.Add(f => f.Label, "Workspace name").Add(f => f.Horizontal, true)
            .AddUnmatched("class", "host").AddUnmatched("data-section", "general")
            .AddChildContent<FlTextInput>(i => Bind(i, () => _model.Name)));
        var field = cut.Find("div[data-section='general']");
        Assert.Equal("fl-field fl-field--horizontal host", field.ClassName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ALabelIsRequired(string label)
    {
        var error = Assert.Throws<InvalidOperationException>(() => _context.Render<FlField>(p => p.Add(f => f.Label, label)
            .AddChildContent<FlTextInput>(i => Bind(i, () => _model.Name))));
        Assert.Contains("Label", error.Message, StringComparison.Ordinal);
    }

    public void Dispose() => _context.Dispose();

    private static ComponentParameterCollectionBuilder<FlTextInput> Bind(ComponentParameterCollectionBuilder<FlTextInput> p,
        Expression<Func<string?>> expression)
    {
        var getter = expression.Compile();
        return p.Add(i => i.Value, getter()).Add(i => i.ValueChanged, _ => { }).Add(i => i.ValueExpression, expression);
    }

    private static void Field(RenderTreeBuilder builder, string label, string help, Expression<Func<string?>> expression)
    {
        builder.OpenComponent<FlField>(0);
        builder.AddComponentParameter(1, nameof(FlField.Label), label);
        builder.AddComponentParameter(2, nameof(FlField.Help), help);
        builder.AddComponentParameter(3, nameof(FlField.ChildContent), (RenderFragment)(inner => Input(inner, 0, expression, null)));
        builder.CloseComponent();
    }

    private static void Input(RenderTreeBuilder builder, int sequence, Expression<Func<string?>> expression, string? ariaLabel)
    {
        builder.OpenComponent<FlTextInput>(sequence);
        builder.AddComponentParameter(sequence + 1, nameof(FlTextInput.Value), expression.Compile()());
        builder.AddComponentParameter(sequence + 2, nameof(FlTextInput.ValueChanged), EventCallback.Factory.Create<string?>(new object(), _ => { }));
        builder.AddComponentParameter(sequence + 3, nameof(FlTextInput.ValueExpression), expression);
        if (ariaLabel is not null)
        {
            builder.AddAttribute(sequence + 4, "aria-label", ariaLabel);
        }
        builder.CloseComponent();
    }

    internal sealed class Project
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Company { get; set; }
        public string? Width { get; set; }
        public string? Height { get; set; }
    }

    internal sealed class Siblings : ComponentBase
    {
        [Parameter] public Project[] Items { get; set; } = [];

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            foreach (Project item in Items)
            {
                builder.OpenComponent<FlField>(0);
                builder.SetKey(item);
                builder.AddComponentParameter(1, nameof(FlField.Label), "Name");
                builder.AddComponentParameter(2, nameof(FlField.ChildContent), (RenderFragment)(inner => Input(inner, 0, () => item.Name, null)));
                builder.CloseComponent();
            }
        }
    }
}
