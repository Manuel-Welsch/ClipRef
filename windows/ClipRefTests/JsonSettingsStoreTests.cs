using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Integration tests for <see cref="JsonSettingsStore"/> — these touch the disk, writing to a
/// unique temp file that is deleted afterwards. Kept separate from the pure unit tests; they
/// never touch the real %APPDATA% location.
/// </summary>
public class JsonSettingsStoreTests
{
    private static string TempPath() =>
        Path.Combine(Path.GetTempPath(), "ClipRefTest-" + Guid.NewGuid().ToString("N") + ".json");

    [Fact]
    public void PersistsValueAcrossInstances()
    {
        var path = TempPath();
        try
        {
            new JsonSettingsStore(path).Set("logFolderPath", @"D:\clips");
            var reloaded = new JsonSettingsStore(path);
            Assert.Equal(@"D:\clips", reloaded.Get("logFolderPath"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingKeyReturnsNull()
    {
        var path = TempPath();
        try
        {
            new JsonSettingsStore(path).Set("logFolderPath", @"D:\clips");
            Assert.Null(new JsonSettingsStore(path).Get("retentionDays"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void MissingFileYieldsEmptyStoreWithoutThrowing()
    {
        var path = TempPath();   // never created
        var store = new JsonSettingsStore(path);
        Assert.Null(store.Get("anything"));
        Assert.False(File.Exists(path));   // construction does not create the file
    }

    [Fact]
    public void LatestValueWinsAfterReload()
    {
        var path = TempPath();
        try
        {
            var store = new JsonSettingsStore(path);
            store.Set("retentionDays", "7");
            store.Set("retentionDays", "30");
            Assert.Equal("30", new JsonSettingsStore(path).Get("retentionDays"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
