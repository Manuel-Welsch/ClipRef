using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Integration coverage for self-cleaning over the real persistence layer — the on-disk path
/// ADR-0006/0007 deferred from <see cref="ClipboardSaveServiceTests"/> (which fakes
/// <see cref="IFileSystem"/>/<see cref="IFileTagger"/>). Mirrors the macOS
/// <c>testPruneDeletesOnlyOurExpiredFiles</c> end-to-end: a real <see cref="FileSystem"/> enumerate +
/// delete and a real <see cref="NtfsFileTagger"/> ADS round-trip drive
/// <see cref="ClipboardSaveService.PruneOldFiles"/> over a real temp folder. A fixed <b>UTC</b> clock
/// matches the tagger's UTC-stored instants so the cutoff comparison is deterministic (see the note
/// in <c>ClipboardSaveService.PruneOldFiles</c>). <c>[Trait Category=Integration]</c>; skipped on a
/// volume without ADS support.
/// </summary>
[Trait("Category", "Integration")]
public class PruneIntegrationTests
{
    // UTC so the clock basis matches NtfsFileTagger's UTC-stored tag instants. Retention is left at
    // its default of 7 days (logFolderPath is the only seeded setting), mirroring the macOS test.
    private static readonly DateTime FixedClockUtc = new(2026, 6, 25, 13, 30, 45, DateTimeKind.Utc);

    private static ClipboardSaveService Service(string folder)
    {
        var settings = new Settings(new InMemorySettingsStore(("logFolderPath", folder)));
        return new ClipboardSaveService(
            new InMemoryClipboardReader(new ClipboardSnapshot(null, null, null)),
            new InMemoryClipboardWriter(),
            new FileSystem(),
            new NtfsFileTagger(),
            settings,
            () => FixedClockUtc);
    }

    [Fact]
    public void Prune_DeletesExpired_KeepsRecent_AndNeverTouchesUntagged()
    {
        using var dir = new IntegrationTestSupport.TempDir();
        if (!IntegrationTestSupport.AdsSupported(dir.Path)) return; // ADS unsupported here → nothing to verify

        var tagger = new NtfsFileTagger();

        // Ours, 10 days old → older than the 7-day cutoff → pruned.
        var expired = dir.File("old.txt");
        File.WriteAllText(expired, "a");
        tagger.TagAsSaved(expired, FixedClockUtc.AddDays(-10));

        // Ours, 1 day old → inside the window → kept.
        var fresh = dir.File("new.txt");
        File.WriteAllText(fresh, "b");
        tagger.TagAsSaved(fresh, FixedClockUtc.AddDays(-1));

        // Not ours (never tagged), even though it's a plain file in the folder → must never be touched.
        var foreign = dir.File("user-keepsake.txt");
        File.WriteAllText(foreign, "c");

        Service(dir.Path).PruneOldFiles();

        Assert.False(File.Exists(expired), "expired ClipRef file should be pruned");
        Assert.True(File.Exists(fresh), "recent ClipRef file should survive");
        Assert.True(File.Exists(foreign), "untagged user file must never be pruned");
    }
}
