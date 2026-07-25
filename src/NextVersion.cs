using System.Text.RegularExpressions;

namespace EncyExtensionMcp;

/**
 * Works out the version to publish when the author does not name one ("publish it" instead of
 * "publish it as 0.1.4"): the next patch after the highest existing v-tag, or 0.1.0 for a repo that
 * has never been released. A pre-release tag (v0.2.0-rc.1) resolves to its own release, 0.2.0.
 */
public static class NextVersion
{
    private static readonly Regex Tag = new(@"^v?(\d+)\.(\d+)\.(\d+)(?<pre>[-.][0-9A-Za-z.\-]+)?$",
        RegexOptions.Compiled);

    public static string FromTags(string gitTagOutput)
    {
        (int Major, int Minor, int Patch, bool Pre)? best = null;
        foreach (var line in gitTagOutput.Split('\n'))
        {
            var m = Tag.Match(line.Trim());
            if (!m.Success) continue;
            var v = (Major: int.Parse(m.Groups[1].Value), Minor: int.Parse(m.Groups[2].Value),
                     Patch: int.Parse(m.Groups[3].Value), Pre: m.Groups["pre"].Success);
            // A release outranks its own pre-release; otherwise compare numerically.
            int order = best == null ? 1 : Compare(v, best.Value);
            if (order > 0 || (order == 0 && best!.Value.Pre && !v.Pre))
                best = v;
        }
        if (best == null) return "0.1.0";
        var b = best.Value;
        return b.Pre ? $"{b.Major}.{b.Minor}.{b.Patch}" : $"{b.Major}.{b.Minor}.{b.Patch + 1}";
    }

    private static int Compare((int Major, int Minor, int Patch, bool Pre) a,
                               (int Major, int Minor, int Patch, bool Pre) b)
    {
        if (a.Major != b.Major) return a.Major.CompareTo(b.Major);
        if (a.Minor != b.Minor) return a.Minor.CompareTo(b.Minor);
        return a.Patch.CompareTo(b.Patch);
    }
}
