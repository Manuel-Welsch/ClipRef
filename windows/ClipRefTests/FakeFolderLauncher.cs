using ClipRef;

namespace ClipRefTests;

/// <summary>
/// In-memory <see cref="IFolderLauncher"/> test double — records the path the tray asked to open
/// (instead of launching Explorer), so the open-folder action can be asserted without a real window.
/// </summary>
internal sealed class FakeFolderLauncher : IFolderLauncher
{
    internal bool WasOpened { get; private set; }

    internal string? Opened { get; private set; }

    public void Open(string path)
    {
        WasOpened = true;
        Opened = path;
    }
}
