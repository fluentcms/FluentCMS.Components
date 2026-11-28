using System.Runtime.CompilerServices;
using System.Text;

namespace FluentCMS.Components;

// Ids derived from the bound member, so prerendering and the first interactive render agree. A
// set per service scope (request, circuit or WebAssembly app) keeps siblings bound to the same
// member apart; the scope is the key so that no registration is needed.
internal static class FlIds
{
    private static readonly ConditionalWeakTable<IServiceProvider, HashSet<string>> Scopes = new();

    internal static string Reserve(IServiceProvider scope, string member)
    {
        string stem = "fl-" + Slug(member);
        HashSet<string> taken = Scopes.GetValue(scope, _ => new HashSet<string>(StringComparer.Ordinal));
        lock (taken)
        {
            string id = stem;
            for (int ordinal = 2; !taken.Add(id); ordinal++)
            {
                id = stem + "-" + ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            return id;
        }
    }

    internal static void Release(IServiceProvider scope, string id)
    {
        if (Scopes.TryGetValue(scope, out HashSet<string>? taken))
        {
            lock (taken)
            {
                taken.Remove(id);
            }
        }
    }

    private static string Slug(string member)
    {
        var slug = new StringBuilder(member.Length);
        foreach (char c in member)
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                slug.Append(char.ToLowerInvariant(c));
            }
            else if (slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }
        string result = slug.ToString().TrimEnd('-');
        return result.Length == 0 ? "field" : result;
    }
}
