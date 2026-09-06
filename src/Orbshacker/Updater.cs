using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Orbshacker;

public sealed record ReleaseAsset([property: JsonPropertyName("name")] string Name, [property: JsonPropertyName("browser_download_url")] string DownloadUrl);
public sealed record GitHubRelease([property: JsonPropertyName("tag_name")] string Tag, [property: JsonPropertyName("assets")] List<ReleaseAsset> Assets);

public sealed class Updater(ApiClient api)
{
    public async Task CheckAsync()
    {
        if (Program.IsFakeGame || !OperatingSystem.IsWindows() || !string.IsNullOrEmpty(Assembly.GetExecutingAssembly().Location)) return;
        var old = Path.ChangeExtension(Environment.ProcessPath!, ".old");
        try { if (File.Exists(old)) File.Delete(old); } catch { }
        try
        {
            var release = await api.GetJsonAsync<GitHubRelease>($"https://api.github.com/repos/{AppConfig.Repository}/releases/latest");
            if (!Version.TryParse(release.Tag.TrimStart('v'), out var remote) || !Version.TryParse(AppConfig.Version, out var local) || remote <= local) return;
            var asset = FindExecutableAsset(release.Assets);
            if (asset is null) { Ui.Warning($"v{remote} is available but has no executable asset."); return; }
            Ui.Info($"Downloading update {local} -> {remote}...");
            var downloaded = Path.Combine(Path.GetTempPath(), $"orbshacker-{Guid.NewGuid():N}.exe"); await api.DownloadAsync(asset.DownloadUrl, downloaded);
            Apply(downloaded);
        }
        catch (Exception error) { Ui.Warning($"Update check failed: {error.Message}"); }
    }

    private static void Apply(string downloaded)
    {
        var current = Environment.ProcessPath!; var old = Path.ChangeExtension(current, ".old"); var script = Path.Combine(Path.GetTempPath(), $"orbshacker-update-{Guid.NewGuid():N}.cmd");
        File.WriteAllText(script, $"@echo off\r\ntimeout /t 2 /nobreak >nul\r\ndel /f /q \"{old}\" >nul 2>&1\r\nmove /y \"{current}\" \"{old}\" >nul || goto fail\r\nmove /y \"{downloaded}\" \"{current}\" >nul || goto restore\r\nstart \"\" \"{current}\"\r\n:deleteold\r\ndel /f /q \"{old}\" >nul 2>&1\r\nif exist \"{old}\" (timeout /t 1 /nobreak >nul & goto deleteold)\r\ngoto done\r\n:restore\r\nmove /y \"{old}\" \"{current}\" >nul\r\n:fail\r\n:done\r\ndel /f /q \"%~f0\"\r\n");
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{script}\"") { UseShellExecute = false, CreateNoWindow = true }); Environment.Exit(0);
    }

    public static ReleaseAsset? FindExecutableAsset(IEnumerable<ReleaseAsset> assets) => assets.FirstOrDefault(asset => asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
    public static string Sha256(string path)
    {
        using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
