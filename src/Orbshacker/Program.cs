namespace Orbshacker;

internal static class Program
{
    public static bool IsFakeGame => !Path.GetFileNameWithoutExtension(Environment.ProcessPath).Equals("orbshacker", StringComparison.OrdinalIgnoreCase);

    [STAThread]
    private static async Task Main()
    {
        AppConfig.Load();
        if (IsFakeGame)
        {
            TimerForm.HideConsole(); ApplicationConfiguration.Initialize(); Application.Run(new TimerForm(AppConfig.Settings.TimerMinutes)); return;
        }

        var api = new ApiClient(); await new Updater(api).CheckAsync(); Ui.Banner(); Ui.Info("Connecting to Discord API...");
        var database = new DiscordGamesDatabase(api);
        try { await database.LoadAsync(); } catch (DatabaseLoadException error) { Ui.Error(error.Message); return; }
        using var faker = new GameFaker(); var steam = new SteamService(api);
        while (true)
        {
            Console.Clear(); Ui.Banner(); Ui.Write($"Active Database: {database.Source} ({database.Games.Count} games)"); Ui.Menu(); Console.Write("Select option [1-5]: ");
            switch (Console.ReadLine()?.Trim())
            {
                case "1": DatabaseMode(database, faker); break;
                case "2": ManualMode(faker); break;
                case "3": await SteamMode(steam, faker); break;
                case "4": Credits(); break;
                case "5": Ui.Info("Thanks for using orbshacker!"); return;
                default: Ui.Error("Invalid option. Press Enter to retry."); Console.ReadLine(); break;
            }
        }
    }

    private static void DatabaseMode(DiscordGamesDatabase database, GameFaker faker)
    {
        Ui.BoxedTitle("DATABASE SEARCH");
        Console.Write("Search (or 'back'): "); var query = Console.ReadLine()?.Trim(); if (string.IsNullOrEmpty(query) || query is "back" or "b") return;
        var games = database.SearchGames(query); if (games.Count == 0) { Ui.Error("No games found."); Pause(); return; }
        for (var index = 0; index < games.Count; index++) Console.WriteLine($"{index + 1,2}. {games[index].Name}  ({string.Join(", ", games[index].Aliases ?? [])})");
        Console.Write($"Select [1-{games.Count}] (or 'back'): ");
        if (!int.TryParse(Console.ReadLine(), out var selected) || selected < 1 || selected > games.Count) return;
        var game = games[selected - 1]; var executable = database.GetWin32Executable(game);
        if (executable is null) { Console.Write("No Windows executable found. Enter one manually: "); executable = PathUtilities.SanitizeRelativePath(Console.ReadLine()); }
        var allExecutables = database.GetAllExecutables(game);
        Console.WriteLine($"Game: {game.Name}\nID: {game.Id}\nExecutable: {executable}\nPath: {Path.Combine(faker.ChosenPath, AppConfig.Settings.FakeExeDir, executable)}");
        if (allExecutables.Count > 1) Console.WriteLine($"Other executables: {string.Join(", ", allExecutables.Skip(1).Take(2))}{(allExecutables.Count > 3 ? $" (+{allExecutables.Count - 3} more)" : "")}");
        if (Ui.Confirm()) CreateAndLaunch(faker, executable); Pause();
    }

    private static void ManualMode(GameFaker faker)
    {
        Ui.BoxedTitle("MANUAL MODE");
        Console.Write("Exact executable name (or 'back'): "); var executable = Console.ReadLine()?.Trim(); if (string.IsNullOrEmpty(executable) || executable is "back" or "b") return;
        Console.WriteLine($"Path: {Path.Combine(faker.ChosenPath, AppConfig.Settings.FakeExeDir, executable)}"); if (Ui.Confirm()) CreateAndLaunch(faker, executable); Pause();
    }

    private static async Task SteamMode(SteamService steam, GameFaker faker)
    {
        Ui.BoxedTitle("STEAM QUEST MODE", 55);
        var steamPath = steam.GetSteamPath();
        if (steamPath is null || !Directory.Exists(steamPath))
        {
            Ui.Warning("Could not locate Steam automatically."); Console.Write("Enter Steam path manually (or leave empty to abort): "); steamPath = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(steamPath)) { Ui.Error("No Steam path provided."); Pause(); return; }
        }
        Console.Write("Steam game name (or 'back'): "); var query = Console.ReadLine()?.Trim(); if (string.IsNullOrEmpty(query) || query is "back" or "b") return;
        IReadOnlyList<SteamStoreItem> results;
        try { results = await steam.SearchAsync(query); } catch (NetworkException error) { Ui.Error(error.Message); Pause(); return; }
        if (results.Count == 0) { Ui.Error("No Steam games found."); Pause(); return; }
        for (var index = 0; index < results.Count; index++) Console.WriteLine($"{index + 1,2}. {results[index].Name} ({results[index].Id})");
        Console.Write("Select game: "); if (!int.TryParse(Console.ReadLine(), out var selected) || selected < 1 || selected > results.Count) return;
        var item = results[selected - 1]; var info = await steam.FetchAppInfoAsync(item.Id);
        if (info is null)
        {
            Ui.Warning("Could not fetch app info automatically. Enter details manually.");
            Console.Write("Game name: "); var name = Console.ReadLine()?.Trim();
            Console.Write("Install dir (folder in steamapps/common): "); var install = Console.ReadLine()?.Trim();
            Console.Write("Executable (e.g. Bin/Game.exe): "); var manualExe = Console.ReadLine()?.Trim();
            info = new SteamAppInfo(PathUtilities.SanitizeFileName(string.IsNullOrEmpty(name) ? $"App {item.Id}" : name), PathUtilities.SanitizePathSegment(string.IsNullOrEmpty(install) ? $"App{item.Id}" : install), PathUtilities.SanitizeRelativePath(string.IsNullOrEmpty(manualExe) ? "Game.exe" : manualExe), null);
        }
        Console.WriteLine($"Install directory: {info.InstallDirectory}\nExecutable: {info.Executable}"); Console.Write("Override executable path [Enter to keep]: ");
        var custom = Console.ReadLine()?.Trim(); var executable = string.IsNullOrEmpty(custom) ? info.Executable : PathUtilities.SanitizeRelativePath(custom);
        var destination = Path.Combine(steamPath, "steamapps", "common", info.InstallDirectory, executable.Replace('/', Path.DirectorySeparatorChar));
        Console.WriteLine($"Manifest: {Path.Combine(steamPath, "steamapps", $"appmanifest_{item.Id}.acf")}\nFake exe: {destination}"); if (!Ui.Confirm()) return;
        try
        {
            var manifest = steam.GenerateAppManifest(item.Id, info.Name, info.InstallDirectory, steamPath, info.DepotId); faker.RegisterCreatedFile(manifest);
            faker.CopyExecutableTo(destination, manifest); Ui.Success($"Created {destination}"); faker.LaunchExecutable(destination);
        }
        catch (Exception error) { Ui.Error($"Steam setup failed: {error.Message}"); }
        Pause();
    }

    private static void CreateAndLaunch(GameFaker faker, string executable)
    {
        try { var path = faker.CreateFakeGame(executable); Ui.Success($"Created {path}"); faker.LaunchExecutable(path); }
        catch (Exception error) { Ui.Error($"Failed to create executable: {error.Message}"); }
    }
    private static void Credits()
    {
        Ui.BoxedTitle("CREDITS", 65);
        Console.WriteLine($"Developer: {AppConfig.Developer}\nVersion: {AppConfig.Version}\n\nConnects to Discord's detectable-app API, creates a process with the expected name, and keeps it running through a graphical timer. Steam Quest Mode additionally generates a partial-download appmanifest and places the executable in steamapps/common.\n\nDiscord must be running for detection. Educational use only; users are solely responsible for compliance with applicable terms."); Pause();
    }
    private static void Pause() { Console.Write("\nPress Enter to continue..."); Console.ReadLine(); }
}
