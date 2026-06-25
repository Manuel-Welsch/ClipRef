using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Tests the WinForms-free tray controller <see cref="TrayActions"/> — the unit-tested boundary of
/// the otherwise integration-only menu host (ADR-0009). It drives the three menu actions over the
/// same in-memory doubles as <see cref="ClipboardSaveServiceTests"/>, plus a fake folder launcher and
/// picker, with no <see cref="System.Windows.Forms.NotifyIcon"/> and no modal dialog. The deep
/// save/prune behavior is pinned in <see cref="ClipboardSaveServiceTests"/>; here we verify the menu
/// wiring: open creates-then-opens, change persists only a real selection (and the shared
/// <see cref="Settings"/> reaches the service), and save-now delegates to <see cref="ClipboardSaveService.Save"/>.
/// </summary>
public class TrayActionsTests
{
    private const string Folder = @"C:\logs";

    private static readonly DateTime FixedClock = new(2026, 6, 25, 13, 30, 45);

    private sealed record Harness(
        TrayActions Actions,
        ClipboardSaveService Service,
        InMemoryFileSystem FileSystem,
        InMemoryClipboardWriter Writer,
        Settings Settings,
        FakeFolderLauncher Launcher,
        FakeFolderPicker Picker);

    /// <summary>
    /// Wires <see cref="TrayActions"/> over one shared <see cref="Settings"/> and
    /// <see cref="InMemoryFileSystem"/> — the same instances the service uses — so a folder change
    /// made through the controller is visible to a subsequent <see cref="ClipboardSaveService.Save"/>.
    /// </summary>
    private static Harness BuildHarness(
        ClipboardSnapshot? clipboard = null,
        InMemoryFileSystem? fileSystem = null,
        FakeFolderPicker? picker = null,
        string folder = Folder)
    {
        fileSystem ??= new InMemoryFileSystem();
        picker ??= new FakeFolderPicker(null);
        var launcher = new FakeFolderLauncher();
        var settings = new Settings(new InMemorySettingsStore(("logFolderPath", folder)));
        var writer = new InMemoryClipboardWriter();
        var service = new ClipboardSaveService(
            new InMemoryClipboardReader(clipboard ?? new ClipboardSnapshot(null, null, null)),
            writer, fileSystem, new InMemoryFileTagger(), settings, () => FixedClock);
        var actions = new TrayActions(service, settings, fileSystem, launcher, picker);
        return new Harness(actions, service, fileSystem, writer, settings, launcher, picker);
    }

    [Fact]
    public void OpenFolder_CreatesDestinationFolderThenOpensIt()
    {
        var harness = BuildHarness();

        harness.Actions.OpenFolder();

        Assert.Contains(Folder, harness.FileSystem.CreatedDirectories); // created first (parity with makeDestinationFolder)
        Assert.True(harness.Launcher.WasOpened);
        Assert.Equal(Folder, harness.Launcher.Opened);                  // then opened that same folder
    }

    [Fact]
    public void OpenFolder_StillOpensWhenFolderCreateFails()
    {
        var fileSystem = new InMemoryFileSystem { ThrowOnCreateDirectory = true };
        var harness = BuildHarness(fileSystem: fileSystem);

        var exception = Record.Exception(() => harness.Actions.OpenFolder());

        Assert.Null(exception);                       // best-effort create never escapes
        Assert.True(harness.Launcher.WasOpened);       // and the folder is still opened
        Assert.Equal(Folder, harness.Launcher.Opened);
    }

    [Fact]
    public void ChangeFolder_PersistsChosenFolder()
    {
        const string picked = @"C:\picked";
        var harness = BuildHarness(
            clipboard: new ClipboardSnapshot(null, "hello", null),
            picker: new FakeFolderPicker(picked));

        harness.Actions.ChangeFolder();

        Assert.Equal(Folder, harness.Picker.SeededWith);     // picker seeded with the prior folder
        Assert.Equal(picked, harness.Actions.CurrentFolder); // controller reflects the new folder
        Assert.Equal(picked, harness.Settings.FolderPath);   // persisted to settings

        // The shared Settings reached the service: the next save lands in the new folder.
        var result = harness.Service.Save();
        var expected = Path.Combine(picked, "clip-2026-06-25_13.30.45.txt");
        Assert.Equal<SaveResult>(new SaveResult.Saved(expected), result);
        Assert.Equal("@" + expected, harness.Writer.LastText);
    }

    [Fact]
    public void ChangeFolder_Cancelled_LeavesFolderUnchanged()
    {
        var harness = BuildHarness(picker: new FakeFolderPicker(null)); // null = user cancelled

        harness.Actions.ChangeFolder();

        Assert.Equal(Folder, harness.Picker.SeededWith);
        Assert.Equal(Folder, harness.Actions.CurrentFolder); // unchanged
        Assert.Equal(Folder, harness.Settings.FolderPath);   // not overwritten with an empty path
    }

    [Fact]
    public void SaveNow_SavesClipboardIntoConfiguredFolder()
    {
        var harness = BuildHarness(clipboard: new ClipboardSnapshot(null, "hello world", null));

        harness.Actions.SaveNow();

        var expected = Path.Combine(Folder, "clip-2026-06-25_13.30.45.txt");
        Assert.Equal("hello world", harness.FileSystem.TextAt(expected)); // delegated to Save()
        Assert.Equal("@" + expected, harness.Writer.LastText);            // @-reference put back
    }
}
