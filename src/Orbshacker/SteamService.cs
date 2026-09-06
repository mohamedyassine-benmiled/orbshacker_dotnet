using Microsoft.Win32;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Orbshacker;

public sealed record SteamStoreItem([property: JsonPropertyName("id")] int Id, [property: JsonPropertyName("name")] string Name);
public sealed record SteamAppInfo(string Name, string InstallDirectory, string Executable, string? DepotId);

public sealed class SteamService(ApiClient api)
{
    public string? GetSteamPath()
    {
        if (!OperatingSystem.IsWindows()) return null;
        try
        {
            var configured = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam")?.GetValue("SteamPath")?.ToString();
            if (!string.IsNullOrWhiteSpace(configured)) return configured;
        }
        catch { }
        var fallback = @"C:\Program Files (x86)\Steam";
        return Directory.Exists(fallback) ? fallback : null;
    }

    public string GetSteamUserId()
    {
        if (!OperatingSystem.IsWindows()) return "0";
        try
        {
            var value = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam\ActiveProcess")?.GetValue("ActiveUser");
            return (Convert.ToInt64(value) + 76561197960265728L).ToString();
        }
        catch { return "0"; }
    }

    public async Task<IReadOnlyList<SteamStoreItem>> SearchAsync(string query)
    {
        var url = $"{AppConfig.SteamSearchUrl}?term={Uri.EscapeDataString(query)}&l=english&cc=US";
        using var data = await api.GetJsonAsync<JsonDocument>(url);
        return data.RootElement.TryGetProperty("items", out var items)
            ? JsonSerializer.Deserialize<List<SteamStoreItem>>(items.GetRawText(), AppConfig.JsonOptions) ?? [] : [];
    }

    public async Task<SteamAppInfo?> FetchAppInfoAsync(int appId)
    {
        try
        {
            using var document = await api.GetJsonAsync<JsonDocument>($"{AppConfig.SteamCmdApiUrl}/{appId}");
            var app = document.RootElement.GetProperty("data").GetProperty(appId.ToString());
            var common = app.GetProperty("common"); var config = app.GetProperty("config");
            var rawName = common.TryGetProperty("name", out var n) ? n.GetString() ?? $"App {appId}" : $"App {appId}";
            var install = config.TryGetProperty("installdir", out var i) ? i.GetString() ?? rawName : rawName;
            var executable = config.TryGetProperty("launch", out var launches) ? PickWindowsExecutable(launches) : null;
            var depot = app.TryGetProperty("depots", out var depots) ? depots.EnumerateObject().FirstOrDefault(p => p.Name.All(char.IsDigit)).Name : null;
            install = PathUtilities.SanitizePathSegment(install);
            return new(PathUtilities.SanitizeFileName(rawName), install, PathUtilities.SanitizeRelativePath(executable ?? install + ".exe"), depot);
        }
        catch (Exception error) when (error is NetworkException or KeyNotFoundException or InvalidOperationException or JsonException) { Ui.Warning($"SteamCMD API error: {error.Message}"); return null; }
    }

    public static string? PickWindowsExecutable(JsonElement launches)
    {
        foreach (var launch in launches.EnumerateObject().OrderBy(p => p.Name))
        {
            var entry = launch.Value;
            var os = entry.TryGetProperty("config", out var cfg) && cfg.TryGetProperty("oslist", out var osList) ? osList.GetString() ?? "windows" : "windows";
            var executable = entry.TryGetProperty("executable", out var exe) ? exe.GetString() ?? "" : "";
            if ((os.Length == 0 || os.Contains("windows", StringComparison.OrdinalIgnoreCase)) && executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) return executable.Replace('\\', '/');
        }
        return null;
    }

    public string GenerateAppManifest(int appId, string name, string installDirectory, string steamPath, string? depotId)
    {
        static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        var staged = depotId is null ? "" : $"\n\t\t\"{depotId}\"\n\t\t{{\n\t\t\t\"manifest\"\t\t\"0\"\n\t\t\t\"size\"\t\t\"1073741824\"\n\t\t\t\"dlcappid\"\t\t\"0\"\n\t\t}}";
        var content = $"\"AppState\"\n{{\n\t\"appid\"\t\t\"{appId}\"\n\t\"universe\"\t\t\"1\"\n\t\"LauncherPath\"\t\t\"{Escape(Path.Combine(steamPath, "steam.exe"))}\"\n\t\"name\"\t\t\"{Escape(name)}\"\n\t\"StateFlags\"\t\t\"1026\"\n\t\"installdir\"\t\t\"{Escape(installDirectory)}\"\n\t\"LastUpdated\"\t\t\"0\"\n\t\"LastPlayed\"\t\t\"0\"\n\t\"SizeOnDisk\"\t\t\"0\"\n\t\"StagingSize\"\t\t\"1073741824\"\n\t\"buildid\"\t\t\"0\"\n\t\"LastOwner\"\t\t\"{GetSteamUserId()}\"\n\t\"DownloadType\"\t\t\"1\"\n\t\"UpdateResult\"\t\t\"4\"\n\t\"BytesToDownload\"\t\t\"1073741824\"\n\t\"BytesDownloaded\"\t\t\"27262976\"\n\t\"BytesToStage\"\t\t\"1073741824\"\n\t\"BytesStaged\"\t\t\"27262976\"\n\t\"TargetBuildID\"\t\t\"0\"\n\t\"AutoUpdateBehavior\"\t\t\"0\"\n\t\"AllowOtherDownloadsWhileRunning\"\t\t\"0\"\n\t\"ScheduledAutoUpdate\"\t\t\"0\"\n\t\"InstalledDepots\"\n\t{{\n\t}}\n\t\"StagedDepots\"\n\t{{{staged}\n\t}}\n\t\"UserConfig\"\n\t{{\n\t}}\n\t\"MountedConfig\"\n\t{{\n\t}}\n}}\n";
        var path = Path.Combine(steamPath, "steamapps", $"appmanifest_{appId}.acf");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, content, new UTF8Encoding(false)); return path;
    }
}
