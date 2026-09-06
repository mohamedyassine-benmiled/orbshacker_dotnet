using System.Reflection;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Orbshacker;

public sealed record AppSettings
{
    public string ChosenFolder { get; init; } = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    public bool AutoDelete { get; init; }
    public int TimerMinutes { get; init; } = 15;
    public string FakeExeDir { get; init; } = "Win64";
    public string? SteamManifestPath { get; init; }
}

public static class AppConfig
{
    public const string Developer = "Strykey / Daniel Pires / Pannenkoekisus";
    public const string Repository = "DanielPires2000/orbshacker";
    public const string DiscordApiUrl = "https://discord.com/api/v9/applications/detectable";
    public const string GitHubBackupUrl = "https://gist.githubusercontent.com/Cynosphere/c1e77f77f0e565ddaac2822977961e76/raw/gameslist.json";
    public const string SteamCmdApiUrl = "https://api.steamcmd.net/v1/info";
    public const string SteamSearchUrl = "https://store.steampowered.com/api/storesearch";
    public const string BakedMarker = "__ORBSHACKER_BAKED_CONFIG__";
    public static string Version => ResolveVersion();
    public static AppSettings Settings { get; private set; } = new();

    public static void Load()
    {
        var embedded = ReadEmbeddedSettings(Environment.ProcessPath!);
        if (embedded is not null) { Settings = Normalize(embedded); return; }
        var path = Path.Combine(AppContext.BaseDirectory, "settings.json");
        if (File.Exists(path))
        {
            try { Settings = Normalize(JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions) ?? new()); return; }
            catch (JsonException) { }
        }
        var legacy = Path.Combine(AppContext.BaseDirectory, "settings.py");
        if (File.Exists(legacy)) { Settings = Normalize(ReadLegacySettings(File.ReadAllLines(legacy), Settings)); return; }
        if (!Program.IsFakeGame)
            try { File.WriteAllText(path, JsonSerializer.Serialize(new AppSettings(), JsonOptions)); }
            catch { }
    }

    public static AppSettings? ReadEmbeddedSettings(string executable)
    {
        try
        {
            using var stream = File.OpenRead(executable);
            var length = (int)Math.Min(stream.Length, 65536);
            stream.Seek(-length, SeekOrigin.End);
            var text = Encoding.UTF8.GetString(new BinaryReader(stream).ReadBytes(length));
            var end = text.LastIndexOf(BakedMarker, StringComparison.Ordinal);
            if (end < 0) return null;
            var start = text.LastIndexOf(BakedMarker, end - 1, StringComparison.Ordinal);
            return start < 0 ? null : JsonSerializer.Deserialize<AppSettings>(text[(start + BakedMarker.Length)..end], JsonOptions);
        }
        catch { return null; }
    }

    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper, WriteIndented = true, PropertyNameCaseInsensitive = true };
    public static AppSettings ReadLegacySettings(IEnumerable<string> lines, AppSettings defaults)
    {
        var values = lines.Select(line => line.Split('#')[0].Trim()).Where(line => line.Contains('='))
            .Select(line => line.Split('=', 2)).ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim(), StringComparer.OrdinalIgnoreCase);
        static string Unquote(string value) => value.Trim().Trim('"', '\'');
        var chosen = defaults.ChosenFolder;
        if (values.TryGetValue("CHOSEN_FOLDER", out var folder))
        {
            var marker = "Path.home() /";
            chosen = folder.Contains(marker, StringComparison.Ordinal) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Unquote(folder[(folder.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..])) : Unquote(folder);
        }
        return defaults with
        {
            ChosenFolder = chosen,
            AutoDelete = values.TryGetValue("AUTO_DELETE", out var delete) ? delete.Equals("True", StringComparison.OrdinalIgnoreCase) : defaults.AutoDelete,
            TimerMinutes = values.TryGetValue("TIMER_MINUTES", out var minutes) && int.TryParse(minutes, out var parsed) ? parsed : defaults.TimerMinutes,
            FakeExeDir = values.TryGetValue("FAKE_EXE_DIR", out var dir) ? Unquote(dir) : defaults.FakeExeDir
        };
    }

    private static string ResolveVersion()
    {
        var assembly = Assembly.GetExecutingAssembly().GetName().Version;
        if (assembly is not null && assembly != new Version(0, 0, 0, 0)) return assembly.ToString(3);
        foreach (var arguments in new[] { "describe --tags --exact-match HEAD", "describe --tags --abbrev=0 --match v*" })
            try
            {
                using var process = Process.Start(new ProcessStartInfo("git", arguments) { WorkingDirectory = AppContext.BaseDirectory, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
                var tag = process?.StandardOutput.ReadToEnd().Trim(); process?.WaitForExit(); if (!string.IsNullOrEmpty(tag)) return tag.TrimStart('v');
            }
            catch { }
        return "0.0.0";
    }
    private static AppSettings Normalize(AppSettings value) => value with
    {
        ChosenFolder = string.IsNullOrWhiteSpace(value.ChosenFolder) || value.ChosenFolder.Equals("Desktop", StringComparison.OrdinalIgnoreCase)
            ? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) : value.ChosenFolder,
        TimerMinutes = Math.Max(1, value.TimerMinutes), FakeExeDir = PathUtilities.SanitizeRelativePath(value.FakeExeDir)
    };
}
