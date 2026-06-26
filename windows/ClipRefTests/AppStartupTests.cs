using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Tests the WinForms-free launch seam <see cref="AppStartup.RunLaunchTasks"/> — the unit-tested
/// boundary of the otherwise integration-only app shell (ADR-0008). It drives the launch sequence
/// over in-memory doubles (no <see cref="System.Windows.Forms.NotifyIcon"/>, no message loop) and
/// asserts that launch runs the prune (parity with macOS <c>applicationDidFinishLaunching</c> →
/// <c>saver.pruneOldFiles()</c>). The prune's own tag-and-age rules are pinned in
/// <see cref="ClipboardSaveServiceTests"/>; here we only verify the launch path invokes it.
///
/// Same fixtures as <see cref="ClipboardSaveServiceTests"/>: the default 7-day retention and the
/// fixed clock put the cutoff at FixedClock - 7 days, so a file tagged older than that is expired.
/// The clipboard snapshot is irrelevant — <see cref="AppStartup.RunLaunchTasks"/> never reads it —
/// so it is always the empty snapshot.
/// </summary>
public class AppStartupTests
{
    private const string Folder = @"C:\logs";

    private static readonly DateTime FixedClock = new(2026, 6, 25, 13, 30, 45);

    private static ClipboardSaveService Service(InMemoryFileSystem fileSystem, InMemoryFileTagger tagger)
    {
        var settings = new Settings(new InMemorySettingsStore(("logFolderPath", Folder)));
        return new ClipboardSaveService(
            new InMemoryClipboardReader(new ClipboardSnapshot(null, null, null)),
            new InMemoryClipboardWriter(),
            fileSystem, tagger, settings, () => FixedClock);
    }

    private static InMemoryFileTagger TaggerWith(params (string Path, DateTime SavedAt)[] tags)
    {
        var tagger = new InMemoryFileTagger();
        foreach (var (path, savedAt) in tags)
        {
            tagger.TagAsSaved(path, savedAt);
        }

        return tagger;
    }

    // An already-configured login item: EnableOnFirstRun is a guaranteed no-op, so the prune-focused
    // tests below are not perturbed by the launch sequence's first-run opt-in.
    private static LoginItem NoOpLoginItem() =>
        new(new FakeAutoStart(enabled: true), new Settings(new InMemorySettingsStore(("didConfigureLoginItem", "true"))));

    [Fact]
    public void RunLaunchTasks_DeletesExpiredTaggedFile()
    {
        var fileSystem = new InMemoryFileSystem();
        var expired = Path.Combine(Folder, "old.txt");
        fileSystem.SeedExisting(expired);
        var service = Service(fileSystem, TaggerWith((expired, FixedClock.AddDays(-10))));

        AppStartup.RunLaunchTasks(service, NoOpLoginItem());

        Assert.False(fileSystem.Exists(expired)); // launch-time prune fired
        Assert.True(fileSystem.Deleted(expired));
    }

    [Fact]
    public void RunLaunchTasks_KeepsRecentAndUntaggedFiles()
    {
        var fileSystem = new InMemoryFileSystem();
        var recent = Path.Combine(Folder, "new.txt");
        var foreign = Path.Combine(Folder, "user-keepsake.txt");
        fileSystem.SeedExisting(recent);
        fileSystem.SeedExisting(foreign); // present but never tagged → not one of ours
        var service = Service(fileSystem, TaggerWith((recent, FixedClock.AddDays(-1))));

        AppStartup.RunLaunchTasks(service, NoOpLoginItem());

        Assert.True(fileSystem.Exists(recent));  // within retention → kept
        Assert.True(fileSystem.Exists(foreign)); // untagged → never touched (not a blanket wipe)
    }

    [Fact]
    public void RunLaunchTasks_EmptyOrMissingFolder_NoOp()
    {
        var fileSystem = new InMemoryFileSystem(); // nothing in the folder
        var service = Service(fileSystem, new InMemoryFileTagger());

        var exception = Record.Exception(() => AppStartup.RunLaunchTasks(service, NoOpLoginItem()));

        Assert.Null(exception); // launch never throws on an empty/missing folder
    }

    [Fact]
    public void RunLaunchTasks_OnFirstRun_EnablesLoginItemAndSetsFlag()
    {
        var fileSystem = new InMemoryFileSystem(); // empty folder → prune no-ops
        var service = Service(fileSystem, new InMemoryFileTagger());
        var autoStart = new FakeAutoStart(enabled: false);
        var settings = new Settings(new InMemorySettingsStore()); // didConfigureLoginItem unset
        var loginItem = new LoginItem(autoStart, settings);

        AppStartup.RunLaunchTasks(service, loginItem);

        Assert.True(autoStart.IsEnabled);            // launch wired the first-run opt-in
        Assert.True(settings.DidConfigureLoginItem); // and recorded that it configured the item
    }
}
