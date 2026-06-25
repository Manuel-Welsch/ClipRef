using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ClipRefTests")]

namespace ClipRef;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Single-instance guard: a second launch finds the named mutex already owned and exits
        // silently, so there is only ever one tray agent (and one launch-time prune). macOS gets
        // this free from LSUIElement/Launch Services; on Windows a named mutex is the equivalent.
        // The session-local name keeps it per-user. The using-var holds the mutex for the whole run.
        using var instanceGuard = new Mutex(initiallyOwned: true, @"Local\ClipRef-SingleInstance", out var createdNew);
        if (!createdNew)
        {
            return;
        }

        ApplicationConfiguration.Initialize();

        var (service, actions, feedback) = CompositionRoot.CreateTrayApp();
        AppStartup.RunLaunchTasks(service);

        Application.Run(new TrayApplicationContext(actions, feedback));
    }
}
