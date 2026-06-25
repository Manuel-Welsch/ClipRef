using System.Diagnostics;

namespace ClipRef;

/// <summary>
/// Production <see cref="IFolderLauncher"/> — shell-opens a folder in Explorer. Integration-only
/// (touches the OS shell), like the other production adapters. <c>UseShellExecute</c> opens the path
/// through the shell, which handles folders with spaces without manual quoting.
/// </summary>
internal sealed class ExplorerFolderLauncher : IFolderLauncher
{
    public void Open(string path)
    {
        // Process.Start returns the launched Process (or null); we don't track the Explorer window.
        _ = Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }
}
