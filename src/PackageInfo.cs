using System.Text.Json;

namespace EncyExtensionMcp;

public static class PackageInfo
{
    /** packageId from src/package.info.json of an extension repo; null when absent/unreadable. */
    public static string? ReadPackageId(string repoDir)
    {
        var path = Path.Combine(repoDir, "src", "package.info.json");
        if (!File.Exists(path)) return null;
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("packageId", out var id) ? id.GetString() : null;
        }
        catch (JsonException) { return null; }
    }
}
