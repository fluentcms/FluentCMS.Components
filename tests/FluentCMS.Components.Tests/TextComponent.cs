using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace FluentCMS.Components.Tests;

public sealed class TextComponent : ComponentBase
{
    [Inject]
    private IServiceProvider Services { get; set; } = null!;

    [Parameter]
    public FlText Text { get; set; }

    [Parameter]
    public object?[] Arguments { get; set; } = [];

    protected override void BuildRenderTree(RenderTreeBuilder builder) =>
        builder.AddContent(0, Services.GetText(Text, Arguments));
}
