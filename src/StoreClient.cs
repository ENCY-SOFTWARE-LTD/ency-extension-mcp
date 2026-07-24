using System.Net;
using System.Text.Json;

namespace EncyExtensionMcp;

public record StoreCard(string Slug, bool Approved, bool Unlisted, string? LatestVersion)
{
    public string CardUrl(string storeBase) => $"{storeBase}/extension/{Slug}";
}

public interface IStoreClient
{
    /** Card by slug or packageId; null when the store has no such extension (yet). */
    Task<StoreCard?> GetCard(string slugOrPackageId);
    string StoreBaseUrl { get; }
}

/** Reads the public store REST API (no auth needed for cards). */
public class StoreClient : IStoreClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    /** API base; override with ENCY_STORE_API for test stands. */
    private readonly string _apiBase =
        (Environment.GetEnvironmentVariable("ENCY_STORE_API") ?? "https://dmc.encycam.com/store/api").TrimEnd('/');

    public string StoreBaseUrl => _apiBase.EndsWith("/api") ? _apiBase[..^4] : _apiBase;

    public async Task<StoreCard?> GetCard(string slugOrPackageId)
    {
        var resp = await Http.GetAsync($"{_apiBase}/extensions/{Uri.EscapeDataString(slugOrPackageId)}");
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var r = doc.RootElement;
        return new StoreCard(
            r.GetProperty("slug").GetString()!,
            r.TryGetProperty("approved", out var a) && a.GetBoolean(),
            r.TryGetProperty("unlisted", out var u) && u.GetBoolean(),
            r.TryGetProperty("latestVersion", out var v) ? v.GetString() : null);
    }
}
