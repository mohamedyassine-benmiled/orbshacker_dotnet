using System.Text;
using System.Text.Json;
using Xunit;

namespace Orbshacker.Tests;

public class AppConfigTests
{
    [Fact]
    public void ReadsSettingsAppendedToExecutable()
    {
        var path = Path.GetTempFileName();
        try
        {
            var settings = new AppSettings { ChosenFolder = "BakedDir", AutoDelete = true, TimerMinutes = 45 };
            File.WriteAllBytes(path, Encoding.UTF8.GetBytes("MZ" + AppConfig.BakedMarker + JsonSerializer.Serialize(settings, AppConfig.JsonOptions) + AppConfig.BakedMarker));
            var result = AppConfig.ReadEmbeddedSettings(path);
            Assert.NotNull(result); Assert.Equal("BakedDir", result.ChosenFolder); Assert.True(result.AutoDelete); Assert.Equal(45, result.TimerMinutes);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void ReadsLegacyPythonSettings()
    {
        var result = AppConfig.ReadLegacySettings(["CHOSEN_FOLDER = 'D:/Games'", "AUTO_DELETE = True", "TIMER_MINUTES = 25", "FAKE_EXE_DIR = 'Fake'"], new AppSettings());
        Assert.Equal("D:/Games", result.ChosenFolder); Assert.True(result.AutoDelete); Assert.Equal(25, result.TimerMinutes); Assert.Equal("Fake", result.FakeExeDir);
    }
}
