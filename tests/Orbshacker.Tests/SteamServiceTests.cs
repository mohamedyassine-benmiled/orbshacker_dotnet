using System.Text.Json;
using Xunit;

namespace Orbshacker.Tests;

public class SteamServiceTests
{
    [Fact]
    public void PicksFirstWindowsExecutableAndNormalizesSlashes()
    {
        using var json = JsonDocument.Parse("""{"1":{"executable":"mac.app","config":{"oslist":"macos"}},"0":{"executable":"Bin\\Game.exe","config":{"oslist":"windows"}}}""");
        Assert.Equal("Bin/Game.exe", SteamService.PickWindowsExecutable(json.RootElement));
    }

    [Fact]
    public void GeneratedManifestMatchesPartialDownloadFields()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var path = new SteamService(new ApiClient()).GenerateAppManifest(123, "Game", "GameDir", root, "456");
            var content = File.ReadAllText(path);
            Assert.Contains("\"StateFlags\"\t\t\"1026\"", content);
            Assert.Contains("\"AutoUpdateBehavior\"\t\t\"0\"", content);
            Assert.Contains("\"AllowOtherDownloadsWhileRunning\"\t\t\"0\"", content);
            Assert.Contains("\"ScheduledAutoUpdate\"\t\t\"0\"", content);
            Assert.Contains("\"456\"", content);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
