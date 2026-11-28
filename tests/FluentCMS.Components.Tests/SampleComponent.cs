using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace FluentCMS.Components.Tests;

// Stands in for a library component until the first one exists.
public sealed class SampleComponent : ComponentBase
{
    [Parameter]
    public string Label { get; set; } = "";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "type", "button");
        builder.AddAttribute(2, "class", "fl-sample");
        builder.AddContent(3, Label);
        builder.CloseElement();
    }
}
