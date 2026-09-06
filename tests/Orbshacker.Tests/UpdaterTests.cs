using Xunit;

namespace Orbshacker.Tests;

public class UpdaterTests
{
    [Fact]
    public void FindsExecutableAssetCaseInsensitively()
    {
        var asset = Updater.FindExecutableAsset([new("notes.txt", "a"), new("orbshacker.EXE", "b")]);
        Assert.NotNull(asset); Assert.Equal("b", asset.DownloadUrl);
    }

    [Fact]
    public void ComputesStableSha256()
    {
        var path = Path.GetTempFileName();
        try { File.WriteAllText(path, "hello world"); Assert.Equal("b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9", Updater.Sha256(path)); }
        finally { File.Delete(path); }
    }
}
