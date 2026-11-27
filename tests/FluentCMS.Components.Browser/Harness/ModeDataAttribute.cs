using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

namespace FluentCMS.Components.Browser.Harness;

// One row of a theory for each render mode. A test names the modes it applies to, or none for all three.
// A row whose mode the run leaves out (FLUENTCMS_MODES) is reported as skipped, so a filter never
// leaves a test with no rows. With Pages, a row is a mode and a page of the gallery.
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ModeDataAttribute(params RenderMode[] modes) : DataAttribute
{
    private readonly RenderMode[] _modes = modes.Length == 0 ? [.. Enum.GetValues<RenderMode>()] : modes;

    public string[]? Pages { get; set; }

    public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(MethodInfo testMethod, DisposalTracker disposalTracker)
    {
        List<ITheoryDataRow> rows = [];
        foreach (RenderMode mode in _modes)
        {
            string? skip = RunSettings.Modes.Contains(mode) ? null : $"{RunSettings.ModesVariable} leaves out {mode}.";
            if (Pages is null)
            {
                rows.Add(new TheoryDataRow(mode) { Skip = skip });
                continue;
            }

            foreach (string page in Pages)
            {
                rows.Add(new TheoryDataRow(mode, page) { Skip = skip });
            }
        }

        return new ValueTask<IReadOnlyCollection<ITheoryDataRow>>(rows);
    }

    public override bool SupportsDiscoveryEnumeration() => true;
}
