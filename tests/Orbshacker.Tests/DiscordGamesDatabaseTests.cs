using Xunit;

namespace Orbshacker.Tests;

public class DiscordGamesDatabaseTests
{
    [Fact]
    public void SearchesNameAndAliasesWithExactResultsFirst()
    {
        var db = new DiscordGamesDatabase(new ApiClient());
        typeof(DiscordGamesDatabase).GetProperty(nameof(db.Games))!.SetValue(db, new List<GameRecord>
        {
            new("1", "Minecraft", ["MC"], [new("win32", "javaw.exe")]),
            new("2", "Minecraft Dungeons", [], [new("win32", "Dungeons.exe")])
        });
        Assert.Equal("1", db.SearchGames("mc")[0].Id);
        Assert.Equal(2, db.SearchGames("mine").Count);
    }

    [Fact]
    public void FiltersNonWindowsAndLauncherExecutables()
    {
        var db = new DiscordGamesDatabase(new ApiClient());
        var game = new GameRecord("3", "Fortnite", [], [new("win32", ">Fortnite.exe"), new("win32", "Launcher.exe"), new("linux", "game")]);
        Assert.Equal("Fortnite.exe", db.GetWin32Executable(game));
        Assert.Contains("Launcher.exe", db.GetAllExecutables(game));
        Assert.DoesNotContain("game", db.GetAllExecutables(game));
    }
}
