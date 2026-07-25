using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EncyExtensionMcp;

/// <summary>One entry-point guide, as registered in guides/_index.json.</summary>
public record GuideInfo(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("cursorRule")] string CursorRule,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("storeType")] string StoreType);

/// <summary>
/// The extension-type skill library: markdown guides compiled into this assembly. Delivered two ways
/// from the same files — the get_extension_guide MCP tool reads them here, and tools/sync-rules.ps1
/// generates the Cursor rules snapshot in the template repo.
/// </summary>
public static class ExtensionGuides
{
    /// <summary>Section headings every type guide must carry (asserted by tests).</summary>
    public static readonly IReadOnlyList<string> RequiredSections = new[]
    {
        "## When to use", "## Register it", "## Interface", "## Skeleton", "## Gotchas", "## Go deeper"
    };

    private static readonly Assembly Asm = typeof(ExtensionGuides).Assembly;
    private static readonly Lazy<(string Stamp, IReadOnlyList<GuideInfo> Guides)> Index = new(LoadIndex);

    /// <summary>Which upstream commit the signatures in these guides were checked against.</summary>
    public static string VerifiedAgainst => Index.Value.Stamp;

    public static IReadOnlyList<GuideInfo> All => Index.Value.Guides;

    /// <summary>Resolve a type by any spelling: operation_popup, operation-popup, OperationPopup.</summary>
    public static GuideInfo? Find(string? type)
    {
        string k = Normalize(type);
        return k.Length == 0 ? null : All.FirstOrDefault(g => Normalize(g.Key) == k);
    }

    public static string? GetMarkdown(string? type)
    {
        var info = Find(type);
        return info is null ? null : ReadResource(info.File);
    }

    /// <summary>Markdown table of every guide — what the tool returns for type "list".</summary>
    public static string RenderList()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# ENCY extension entry points");
        sb.AppendLine();
        sb.AppendLine($"Verified against {VerifiedAgainst}.");
        sb.AppendLine();
        sb.AppendLine("| type | store type | when to use it |");
        sb.AppendLine("|---|---|---|");
        foreach (var g in All)
            sb.AppendLine($"| `{g.Key}` | `{g.StoreType}` | {g.Description} |");
        sb.AppendLine();
        sb.AppendLine("Call get_extension_guide with one of the type values above, or `cookbook`.");
        return sb.ToString();
    }

    private static string Normalize(string? s) =>
        new((s ?? string.Empty).Where(char.IsLetter).Select(char.ToLowerInvariant).ToArray());

    private static (string, IReadOnlyList<GuideInfo>) LoadIndex()
    {
        using var doc = JsonDocument.Parse(ReadResource("_index.json"));
        string stamp = doc.RootElement.GetProperty("verifiedAgainst").GetString() ?? "unknown";
        var guides = doc.RootElement.GetProperty("guides").Deserialize<List<GuideInfo>>()
                     ?? new List<GuideInfo>();
        return (stamp, guides);
    }

    private static string ReadResource(string fileName)
    {
        // csproj embeds guides/* under the logical name EncyExtensionMcp.guides.<file>
        string name = $"EncyExtensionMcp.guides.{fileName}";
        using var stream = Asm.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"embedded guide resource not found: {name}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
