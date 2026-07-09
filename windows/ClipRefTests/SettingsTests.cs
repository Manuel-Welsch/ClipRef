using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Unit tests for <see cref="Settings"/> over an in-memory store (no disk). Mirror the macOS
/// UserDefaults surface: the log-folder path, a retention-days default of 7, the
/// login-item flag, and the resolved destination folder.
/// </summary>
public class SettingsTests
{
    // RetentionDays — default 7 unless a positive stored value overrides it.

    [Fact]
    public void RetentionDaysDefaultsToSevenWhenMissing()
    {
        var settings = new Settings(new InMemorySettingsStore());
        Assert.Equal(7, settings.RetentionDays);
    }

    [Theory]
    [InlineData("0", 7)]      // non-positive falls back
    [InlineData("-3", 7)]
    [InlineData("abc", 7)]    // unparseable falls back
    [InlineData("14", 14)]    // positive wins
    public void RetentionDaysFallsBackToSevenUnlessPositive(string stored, int expected)
    {
        var settings = new Settings(new InMemorySettingsStore(("retentionDays", stored)));
        Assert.Equal(expected, settings.RetentionDays);
    }

    [Fact]
    public void RetentionDaysWritesThroughToStore()
    {
        var store = new InMemorySettingsStore();
        var settings = new Settings(store) { RetentionDays = 30 };
        Assert.Equal(30, settings.RetentionDays);
        Assert.Equal("30", store.Get("retentionDays"));
    }

    // DidConfigureLoginItem — default false.

    [Fact]
    public void DidConfigureLoginItemDefaultsToFalse()
    {
        var settings = new Settings(new InMemorySettingsStore());
        Assert.False(settings.DidConfigureLoginItem);
    }

    [Theory]
    [InlineData("True", true)]
    [InlineData("False", false)]
    [InlineData("yes", false)]   // unparseable → false
    public void DidConfigureLoginItemParsesStoredBool(string stored, bool expected)
    {
        var settings = new Settings(new InMemorySettingsStore(("didConfigureLoginItem", stored)));
        Assert.Equal(expected, settings.DidConfigureLoginItem);
    }

    [Fact]
    public void DidConfigureLoginItemWritesThroughToStore()
    {
        var store = new InMemorySettingsStore();
        var settings = new Settings(store) { DidConfigureLoginItem = true };
        Assert.True(settings.DidConfigureLoginItem);
        Assert.Equal("True", store.Get("didConfigureLoginItem"));
    }

    // LogFolderPath — empty when unset, round-trips through the store.

    [Fact]
    public void LogFolderPathIsEmptyWhenMissing()
    {
        var settings = new Settings(new InMemorySettingsStore());
        Assert.Equal("", settings.LogFolderPath);
    }

    [Fact]
    public void LogFolderPathReadsStoredValue()
    {
        var settings = new Settings(new InMemorySettingsStore(("logFolderPath", @"D:\clips")));
        Assert.Equal(@"D:\clips", settings.LogFolderPath);
    }

    [Fact]
    public void LogFolderPathWritesThroughToStore()
    {
        var store = new InMemorySettingsStore();
        var settings = new Settings(store) { LogFolderPath = @"E:\x" };
        Assert.Equal(@"E:\x", settings.LogFolderPath);
        Assert.Equal(@"E:\x", store.Get("logFolderPath"));
    }

    // FolderPath — resolved: a stored path wins, else the default under the user profile.

    [Fact]
    public void FolderPathDefaultsUnderUserProfile()
    {
        var settings = new Settings(new InMemorySettingsStore());
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Developer",
            "clipboard-logs");
        Assert.Equal(expected, settings.FolderPath);
        Assert.EndsWith(@"Developer\clipboard-logs", settings.FolderPath);
    }

    [Fact]
    public void FolderPathPrefersStoredLogFolder()
    {
        var settings = new Settings(new InMemorySettingsStore(("logFolderPath", @"D:\clips")));
        Assert.Equal(@"D:\clips", settings.FolderPath);
    }
}
