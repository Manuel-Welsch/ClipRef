namespace ClipRef;

/// <summary>
/// The seam through which the tray asks the user to choose a folder, so the menu's change-folder
/// action stays unit-testable without a modal dialog. Returns the chosen absolute path, or
/// <c>null</c> when the user cancels (so the caller persists only a real selection). The production
/// implementation is <see cref="WinFormsFolderPicker"/>; tests use a fake that returns a preset path.
/// </summary>
internal interface IFolderPicker
{
    string? PickFolder(string initialPath);
}
