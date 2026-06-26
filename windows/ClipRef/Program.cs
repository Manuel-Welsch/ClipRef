using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ClipRefTests")]

namespace ClipRef;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        // Headless one-shot mode runs first — before the single-instance mutex, the WinForms init, and
        // the tray — so `--save-once` saves and exits even while a tray instance already owns the mutex
        // (mirrors macOS exiting before app.run()). Gating it behind the mutex would make it silently
        // return without saving. Main is [STAThread], so the headless save runs on the STA thread the
        // WinForms clipboard seams require.
        if (SaveOnceMode.IsRequested(args))
        {
            return SaveOnceMode.Run();
        }

        // Single-instance guard: a second launch finds the named mutex already owned and exits
        // silently, so there is only ever one tray agent (and one launch-time prune). macOS gets
        // this free from LSUIElement/Launch Services; on Windows a named mutex is the equivalent.
        // The session-local name keeps it per-user. The using-var holds the mutex for the whole run.
        using var instanceGuard = new Mutex(initiallyOwned: true, @"Local\ClipRef-SingleInstance", out var createdNew);
        if (!createdNew)
        {
            return 0;
        }

        ApplicationConfiguration.Initialize();

        var (service, actions, feedback) = CompositionRoot.CreateTrayApp();
        AppStartup.RunLaunchTasks(service);

        Application.Run(new TrayApplicationContext(actions, feedback));
        return 0;
    }
}
