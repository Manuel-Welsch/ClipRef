namespace ClipRef;

/// <summary>
/// Production <see cref="IFolderPicker"/> — a modal <see cref="FolderBrowserDialog"/> seeded at the
/// current folder. Integration-only (a WinForms dialog). Mirrors the macOS <c>changeFolder</c>
/// <c>NSOpenPanel</c> (choose a directory, allow creating one, seeded at the current folder).
/// </summary>
internal sealed class WinFormsFolderPicker : IFolderPicker
{
    public string? PickFolder(string initialPath)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose the folder where saved clipboard files will be stored",
            UseDescriptionForTitle = true,
            SelectedPath = initialPath,
            ShowNewFolderButton = true,
        };

        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    }
}
