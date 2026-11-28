using System.Reflection;
using System.Xml.Linq;

namespace FluentCMS.Components.Tests.Foundations;

public sealed class IconSetTests
{
    [Fact]
    public void GeneratedIconsMatchEveryCommittedSpriteSymbol()
    {
        var sprite = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "lucide.svg"));
        var icons = typeof(FlIcons).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(p => p.Name, p => (FlIconDefinition)p.GetValue(null)!);

        foreach (XElement symbol in sprite.Root!.Elements(sprite.Root.Name.Namespace + "symbol"))
        {
            string id = (string)symbol.Attribute("id")!;
            string name = string.Concat(id.Split('-').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
            if (char.IsDigit(name[0]))
            {
                name = "Icon" + name;
            }

            Assert.True(icons.Remove(name, out FlIconDefinition icon), $"Missing FlIcons.{name}");
            Assert.Equal("0 0 24 24", (string?)symbol.Attribute("viewBox"));
            var actual = XElement.Parse($"""<symbol xmlns="{sprite.Root.Name.Namespace}">{icon.Content}</symbol>""");
            Assert.Equal(symbol.Elements(), actual.Elements(), XNode.EqualityComparer);
        }

        Assert.Empty(icons);
    }
}
