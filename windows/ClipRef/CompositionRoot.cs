namespace ClipRef;

/// <summary>
/// The one place the production seams are wired into a live <see cref="ClipboardSaveService"/> (and,
/// for the tray, a <see cref="TrayActions"/> and a <see cref="LoginItem"/> over the same shared
/// <see cref="Settings"/>). Touches the real OS (clipboard, disk, NTFS, registry, settings file), so —
/// like the adapters it composes — it is integration-only, not unit-tested. <see cref="CreateTrayApp"/>
/// serves the tray host; <see cref="CreateSaveService"/> backs the <c>--save-once</c> headless mode
/// (<see cref="SaveOnceMode.Run"/>), which builds no login item — headless does no login configuration.
/// See ADR-0008/0009/0011.
/// </summary>
internal static class CompositionRoot
{
    /// <summary>
    /// Builds the tray app's object graph: one <see cref="Settings"/> and one <see cref="FileSystem"/>
    /// shared by the save service, the menu actions, and the launch-at-login controller, so a folder
    /// changed via the menu is seen by the next save and the first-run flag is shared. Returns the
    /// service (for the launch-time prune), the actions and feedback (for the host), and the login item
    /// (for the launch-time first-run opt-in and the menu's Launch at Login toggle).
    /// </summary>
    internal static (ClipboardSaveService Service, TrayActions Actions, WinFormsSaveFeedback Feedback, LoginItem LoginItem) CreateTrayApp()
    {
        var settings = new Settings(new JsonSettingsStore(JsonSettingsStore.DefaultFilePath));
        var fileSystem = new FileSystem();
        var service = BuildService(settings, fileSystem);
        var feedback = new WinFormsSaveFeedback();
        var actions = new TrayActions(
            service, settings, fileSystem, new ExplorerFolderLauncher(), new WinFormsFolderPicker(), feedback);
        var loginItem = new LoginItem(new RunKeyAutoStart(), settings);
        return (service, actions, feedback, loginItem);
    }

    /// <summary>A standalone save service for the headless <c>--save-once</c> mode (<see cref="SaveOnceMode.Run"/>).</summary>
    internal static ClipboardSaveService CreateSaveService() =>
        BuildService(new Settings(new JsonSettingsStore(JsonSettingsStore.DefaultFilePath)), new FileSystem());

    private static ClipboardSaveService BuildService(Settings settings, IFileSystem fileSystem) =>
        // The clock MUST be UTC: NtfsFileTagger stores the ownership tag as UTC, and prune compares
        // the tag against now − RetentionDays, so the two must share a basis (the Phase-3 contract
        // flagged at ClipboardSaveService.PruneOldFiles). The clip-<timestamp> filename is rendered in
        // the machine's local zone instead — the default TimeZoneInfo.Local display zone — so the name
        // reads as local wall-clock (macOS parity) while the tag and prune stay on UTC.
        new ClipboardSaveService(
            new WinFormsClipboardReader(),
            new WinFormsClipboardWriter(),
            fileSystem,
            new NtfsFileTagger(),
            settings,
            () => DateTime.UtcNow);
}
