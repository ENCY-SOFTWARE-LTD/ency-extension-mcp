using System.Text.Json;

namespace EncyExtensionMcp;

/// <summary>
/// Store tokens without DevTools: `ency-extension-mcp login` performs a Keycloak password
/// grant once (scope offline_access) and stores ONLY the refresh token under %APPDATA%;
/// afterwards fresh access tokens are minted on demand. The ENCY_STORE_TOKEN env var, when
/// set, overrides everything (CI/debug escape hatch).
/// </summary>
public class StoreTokenProvider
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    private readonly string _tokenEndpoint =
        Environment.GetEnvironmentVariable("ENCY_KEYCLOAK_TOKEN_ENDPOINT")
        ?? "https://webservices.encycam.com/keycloak/realms/licsys/protocol/openid-connect/token";

    /** digital-twins has Direct Access Grants enabled today; switch to extension-store once it does too. */
    private readonly string _clientId =
        Environment.GetEnvironmentVariable("ENCY_STORE_CLIENT_ID") ?? "digital-twins";

    private string? _cachedAccess;
    private DateTimeOffset _cachedUntil = DateTimeOffset.MinValue;

    public static string AuthFilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ency-extension-mcp", "auth.json");

    /// <summary>Fresh access token: env override → cached → minted from the stored refresh token.
    /// Null when the user never logged in (callers should point at `ency-extension-mcp login`).</summary>
    public async Task<string?> GetAccessToken()
    {
        var env = Environment.GetEnvironmentVariable("ENCY_STORE_TOKEN");
        if (!string.IsNullOrWhiteSpace(env)) return env.Trim();

        if (_cachedAccess != null && DateTimeOffset.UtcNow < _cachedUntil) return _cachedAccess;

        string? refresh = ReadStoredRefreshToken();
        if (refresh == null) return null;

        var resp = await Http.PostAsync(_tokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _clientId,
            ["refresh_token"] = refresh,
        }));
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                "Store login expired or was revoked - run `ency-extension-mcp login` again. (" + Trim(json) + ")");
        using var doc = JsonDocument.Parse(json);
        _cachedAccess = doc.RootElement.GetProperty("access_token").GetString();
        int expiresIn = doc.RootElement.TryGetProperty("expires_in", out var e) ? e.GetInt32() : 300;
        _cachedUntil = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresIn - 60));
        // Keycloak rotates refresh tokens - keep the newest one.
        if (doc.RootElement.TryGetProperty("refresh_token", out var rt) && rt.GetString() is { Length: > 0 } newRefresh)
            SaveRefreshToken(newRefresh);
        return _cachedAccess;
    }

    /// <summary>Interactive `ency-extension-mcp login`: password grant, stores the refresh token.</summary>
    public async Task<int> LoginInteractive()
    {
        Console.Write("ENCY store login (licsys email): ");
        string? user = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(user)) { Console.Error.WriteLine("no login given"); return 1; }
        Console.Write("Password (hidden): ");
        string pass = ReadHidden();
        Console.WriteLine();

        var resp = await Http.PostAsync(_tokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _clientId,
            ["username"] = user.Trim(),
            ["password"] = pass,
            ["scope"] = "openid offline_access",
        }));
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            Console.Error.WriteLine("Login failed: " + Trim(json));
            return 1;
        }
        using var doc = JsonDocument.Parse(json);
        var refresh = doc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        if (string.IsNullOrEmpty(refresh))
        {
            Console.Error.WriteLine("Keycloak returned no refresh token - cannot stay logged in.");
            return 1;
        }
        SaveRefreshToken(refresh);
        Console.WriteLine("Logged in. Tokens are minted automatically from now on (stored: " + AuthFilePath + ").");
        return 0;
    }

    private static string? ReadStoredRefreshToken()
    {
        try
        {
            if (!File.Exists(AuthFilePath)) return null;
            using var doc = JsonDocument.Parse(File.ReadAllText(AuthFilePath));
            return doc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        }
        catch (Exception) { return null; }
    }

    private static void SaveRefreshToken(string refresh)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(AuthFilePath)!);
        File.WriteAllText(AuthFilePath, JsonSerializer.Serialize(new { refresh_token = refresh }));
    }

    private static string ReadHidden()
    {
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace) { if (sb.Length > 0) sb.Length--; continue; }
            if (!char.IsControl(key.KeyChar)) sb.Append(key.KeyChar);
        }
        return sb.ToString();
    }

    private static string Trim(string s) => s.Length > 300 ? s[..300] : s;
}
