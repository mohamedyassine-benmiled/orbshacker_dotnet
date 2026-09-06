using Xunit;

namespace Orbshacker.Tests;

public class GameFakerTests
{
    [Fact]
    public void DetectsFrameworkDependentRuntimeConfig()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.WindowsDesktop.App","version":"8.0.0"}}}""");
            Assert.True(GameFaker.IsFrameworkDependent(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void DetectsSelfContainedRuntimeConfig()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """{"runtimeOptions":{"tfm":"net8.0","configProperties":{}}}""");
            Assert.False(GameFaker.IsFrameworkDependent(path));
        }
        finally { File.Delete(path); }
    }
}
