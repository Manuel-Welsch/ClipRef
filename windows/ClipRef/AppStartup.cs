namespace ClipRef;

/// <summary>
/// The app's launch sequence. Kept free of any WinForms type — it takes the already-wired
/// <see cref="ClipboardSaveService"/> as a parameter — so the launch-time behavior is unit-testable
/// without a message loop (the Windows analogue of the macOS XCTest-inert guard; see ADR-0008).
/// </summary>
internal static class AppStartup
{
    /// <summary>
    /// Runs the one-time startup tasks: prune expired saved files on launch, parity with the macOS
    /// <c>applicationDidFinishLaunching</c> calling <c>saver.pruneOldFiles()</c>. Best-effort —
    /// <see cref="ClipboardSaveService.PruneOldFiles"/> never throws.
    /// </summary>
    internal static void RunLaunchTasks(ClipboardSaveService service)
    {
        service.PruneOldFiles();
    }
}
