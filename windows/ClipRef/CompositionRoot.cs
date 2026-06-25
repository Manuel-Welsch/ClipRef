namespace ClipRef;

/// <summary>
/// The one place the production seams are wired into a live <see cref="ClipboardSaveService"/>.
/// Touches the real OS (clipboard, disk, NTFS, settings file), so — like the adapters it composes —
/// it is integration-only, not unit-tested. Reused by the tray host (this item) and the
/// <c>--save-once</c> headless mode (a later item). See ADR-0008.
/// </summary>
internal static class CompositionRoot
{
    internal static ClipboardSaveService CreateSaveService()
    {
        var settings = new Settings(new JsonSettingsStore(JsonSettingsStore.DefaultFilePath));

        // The clock MUST be UTC: NtfsFileTagger stores the ownership tag as UTC, and prune compares
        // the tag against now − RetentionDays, so the two must share a basis (the Phase-3 contract
        // flagged at ClipboardSaveService.PruneOldFiles).
        return new ClipboardSaveService(
            new WinFormsClipboardReader(),
            new WinFormsClipboardWriter(),
            new FileSystem(),
            new NtfsFileTagger(),
            settings,
            () => DateTime.UtcNow);
    }
}
