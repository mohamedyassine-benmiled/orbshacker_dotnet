using Xunit;

namespace Orbshacker.Tests;

public class PathUtilitiesTests
{
    [Theory]
    [InlineData("STAR WARS™", "STAR WARS")]
    [InlineData("Game: Subtitle", "Game Subtitle")]
    [InlineData("game.exe. .", "game.exe")]
    [InlineData("CON", "_CON")]
    [InlineData("aux.txt", "_aux.txt")]
    [InlineData("???", "unnamed")]
    public void SanitizesFileNames(string input, string expected) => Assert.Equal(expected, PathUtilities.SanitizeFileName(input));

    [Theory]
    [InlineData("NieR:Automata™/bin:x64/Game®.exe", "NieRAutomata/binx64/Game.exe")]
    [InlineData(@"Game Folder™\Bin\Game®.exe", "Game Folder/Bin/Game.exe")]
    [InlineData(@"C:\Steam\Game.exe", "Steam/Game.exe")]
    [InlineData("../../Games/My™ Game.exe", "Games/My Game.exe")]
    public void SanitizesRelativePaths(string input, string expected) => Assert.Equal(expected, PathUtilities.SanitizeRelativePath(input));
}
