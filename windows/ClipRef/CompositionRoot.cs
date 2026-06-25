namespace ClipRef;

/// <summary>
/// The one place the production seams are wired into a live <see cref="ClipboardSaveService"/> (and,
/// for the tray, a <see cref="TrayActions"/> over the same shared <see cref="Settings"/>). Touches the
/// real OS (clipboard, disk, NTFS, settings file), so — like the adapters it composes — it is
/// integration-only, not unit-tested. <see cref="CreateTrayApp"/> serves the tray host (this item);
/// <see cref="CreateSaveService"/> serves the <c>--save-once</c> headless mode (a later item). See ADR-0008/0009.
/// </summary>
internal static class CompositionRoot
{
    /// <summary>
    /// Builds the tray app's object graph: one <see cref="Settings"/> and one <see cref="FileSystem"/>
    /// shared by the save service and the menu actions, so a folder changed via the menu is seen by the
    /// next save. Returns the service (for the launch-time prune) and the actions (for the host).
    /// </summary>
    internal static (ClipboardSaveService Service, TrayActions Actions) CreateTrayApp()
    {
        var settings = new Settings(new JsonSettingsStore(JsonSettingsStore.DefaultFilePath));
        var fileSystem = new FileSystem();
        var service = BuildService(settings, fileSystem);
        var actions = new TrayActions(service, settings, fileSystem, new ExplorerFolderLauncher(), new WinFormsFolderPicker());
        return (service, actions);
    }

    /// <summary>A standalone save service for the headless <c>--save-once</c> mode (a later item).</summary>
    internal static ClipboardSaveService CreateSaveService() =>
        BuildService(new Settings(new JsonSettingsStore(JsonSettingsStore.DefaultFilePath)), new FileSystem());

    private static ClipboardSaveService BuildService(Settings settings, IFileSystem fileSystem) =>
        // The clock MUST be UTC: NtfsFileTagger stores the ownership tag as UTC, and prune compares
        // the tag against now − RetentionDays, so the two must share a basis (the Phase-3 contract
        // flagged at ClipboardSaveService.PruneOldFiles).
        new ClipboardSaveService(
            new WinFormsClipboardReader(),
            new WinFormsClipboardWriter(),
            fileSystem,
            new NtfsFileTagger(),
            settings,
            () => DateTime.UtcNow);
}
