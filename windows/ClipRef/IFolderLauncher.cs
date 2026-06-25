namespace ClipRef;

/// <summary>
/// The seam through which the tray opens a folder in the OS shell (Explorer), so the menu's
/// open-folder action stays unit-testable without launching a real window. The production
/// implementation is <see cref="ExplorerFolderLauncher"/>; tests use a fake that records the path.
/// Mirrors the other OS seams (<see cref="IFileSystem"/>, <see cref="IClipboardReader"/>).
/// </summary>
internal interface IFolderLauncher
{
    void Open(string path);
}
