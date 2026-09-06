using System.Text.Json.Serialization;

namespace Orbshacker;

public sealed record ExecutableEntry([property: JsonPropertyName("os")] string Os, [property: JsonPropertyName("name")] string Name);
public sealed record GameRecord(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("aliases")] List<string>? Aliases,
    [property: JsonPropertyName("executables")] List<ExecutableEntry>? Executables);

public sealed class DiscordGamesDatabase(ApiClient api)
{
    private static readonly string[] SkipPatterns = ["_be.exe", "_eac.exe", "launcher", "unins", "crash", "report", "update", "setup", "install"];
    public List<GameRecord> Games { get; private set; } = [];
    public string? Source { get; private set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        Ui.Info("Loading games database...");
        try { Games = await api.GetJsonAsync<List<GameRecord>>(AppConfig.DiscordApiUrl, cancellationToken); Source = "Discord Official API"; }
        catch (NetworkException)
        {
            Ui.Warning("Discord API unavailable, using GitHub backup...");
            try { Games = await api.GetJsonAsync<List<GameRecord>>(AppConfig.GitHubBackupUrl, cancellationToken, TimeSpan.FromSeconds(20)); Source = "GitHub Backup"; }
            catch (NetworkException error) { throw new DatabaseLoadException($"Could not load games database from any source: {error.Message}"); }
        }
        Ui.Success($"Loaded {Games.Count} games from {Source}.");
    }

    public IReadOnlyList<GameRecord> SearchGames(string query, int maximum = 20)
    {
        var exact = Games.Where(g => g.Name.Equals(query, StringComparison.OrdinalIgnoreCase) || (g.Aliases?.Any(a => a.Equals(query, StringComparison.OrdinalIgnoreCase)) ?? false));
        var partial = Games.Where(g => g.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || (g.Aliases?.Any(a => a.Contains(query, StringComparison.OrdinalIgnoreCase)) ?? false));
        return exact.Concat(partial).DistinctBy(g => g.Id).Take(maximum).ToList();
    }

    public IReadOnlyList<string> GetAllExecutables(GameRecord game) => Filter(game, false);
    public string? GetWin32Executable(GameRecord game) => Filter(game, true).FirstOrDefault();
    private static List<string> Filter(GameRecord game, bool skipPatterns)
    {
        return (game.Executables ?? []).Where(e => e.Os == "win32").Select(e => e.Name.TrimStart('>'))
            .Select(name => PathUtilities.SanitizeRelativePath(name)).Where(n => !skipPatterns || !SkipPatterns.Any(p => n.Contains(p, StringComparison.OrdinalIgnoreCase)))
            .Distinct(StringComparer.Ordinal).ToList();
    }
}
