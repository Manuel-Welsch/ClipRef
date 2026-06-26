namespace ClipRef;

/// <summary>
/// The app's launch sequence. Kept free of any WinForms type — it takes the already-wired
/// <see cref="ClipboardSaveService"/> and <see cref="LoginItem"/> as parameters — so the launch-time
/// behavior is unit-testable without a message loop (the Windows analogue of the macOS XCTest-inert
/// guard; see ADR-0008/0011).
/// </summary>
internal static class AppStartup
{
    /// <summary>
    /// Runs the one-time startup tasks, in macOS order (<c>applicationDidFinishLaunching</c>):
    /// register at login on first run, then prune expired saved files. Only the tray launch path calls
    /// this — the headless <c>--save-once</c> mode never does — so login configuration never runs
    /// headless, parity with macOS exiting before <c>applicationDidFinishLaunching</c>.
    /// <see cref="ClipboardSaveService.PruneOldFiles"/> never throws; the first-run opt-in is best-effort.
    /// </summary>
    internal static void RunLaunchTasks(ClipboardSaveService service, LoginItem loginItem)
    {
        try
        {
            loginItem.EnableOnFirstRun();
        }
        catch (Exception)
        {
            // Best-effort, parity with the macOS `try? register` — a denied registry write must not
            // crash launch. The first-run flag is still set, so it is not retried on every start.
        }

        service.PruneOldFiles();
    }
}
