using System.Runtime.InteropServices;

namespace ClipRef;

/// <summary>
/// The headless <c>--save-once</c> entry path (parity with macOS <c>main.swift:7-19</c>): detect the flag,
/// save the clipboard once, print the outcome, and exit with a status code, without ever starting the
/// tray. <see cref="IsRequested"/> is the pure, unit-tested flag predicate (exact, case-sensitive, the
/// locked contract); <see cref="Run"/> is the integration-only runner (console attach + the save service +
/// the console writes), untested shell per ADR-0008.
/// </summary>
internal static class SaveOnceMode
{
    private const int AttachParentProcess = -1;

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int dwProcessId);

    /// <summary>
    /// True when the process was launched with the exact, case-sensitive <c>--save-once</c> token —
    /// faithful to the macOS <c>CommandLine.arguments.contains("--save-once")</c>. Any other spelling or
    /// casing launches the tray as normal.
    /// </summary>
    internal static bool IsRequested(string[] args) => args.Contains("--save-once", StringComparer.Ordinal);

    /// <summary>
    /// Saves the clipboard once and returns the process exit code, writing the outcome to the console:
    /// a successful path to stdout (exit 0), an empty/failed message to stderr (exit 2 / 1). Best-effort
    /// attaches the parent console first so the output is visible in the launching terminal. Reuses the
    /// production save service; deliberately runs no separate launch-time prune — only the self-clean
    /// inside a successful <see cref="ClipboardSaveService.Save"/> runs (macOS exits before the launch
    /// tasks).
    /// </summary>
    internal static int Run()
    {
        AttachParentConsole();
        var outcome = SaveOnceOutcome.For(CompositionRoot.CreateSaveService().Save());
        var writer = outcome.ToStandardError ? Console.Error : Console.Out;
        writer.WriteLine(outcome.Text);
        return outcome.ExitCode;
    }

    /// <summary>
    /// Best-effort: attach to the parent process's console so console output reaches the launching
    /// terminal (a WinExe is otherwise detached from it). No parent console — e.g. a double-click — makes
    /// the call return false, which is fine: there is simply nowhere to print. Redirection and pipes work
    /// regardless, since those standard handles are inherited directly.
    /// </summary>
    private static void AttachParentConsole() => AttachConsole(AttachParentProcess);
}
