namespace ClipRef;

/// <summary>
/// The tray menu's actions, kept free of any WinForms type so they are unit-testable: save the
/// clipboard (presenting its outcome), open the destination folder (creating it first), and change the
/// folder via a picker (persisting only a real selection). The OS touch-points — opening Explorer,
/// showing the folder dialog, and presenting save feedback — sit behind the <see cref="IFolderLauncher"/>,
/// <see cref="IFolderPicker"/>, and <see cref="ISaveFeedback"/> seams, so the WinForms host stays the
/// untested shell (ADR-0009/0010).
/// </summary>
internal sealed class TrayActions
{
    private readonly ClipboardSaveService _service;
    private readonly Settings _settings;
    private readonly IFileSystem _fileSystem;
    private readonly IFolderLauncher _folderLauncher;
    private readonly IFolderPicker _folderPicker;
    private readonly ISaveFeedback _feedback;

    internal TrayActions(
        ClipboardSaveService service,
        Settings settings,
        IFileSystem fileSystem,
        IFolderLauncher folderLauncher,
        IFolderPicker folderPicker,
        ISaveFeedback feedback)
    {
        _service = service;
        _settings = settings;
        _fileSystem = fileSystem;
        _folderLauncher = folderLauncher;
        _folderPicker = folderPicker;
        _feedback = feedback;
    }

    /// <summary>The folder saves currently go to — shown in the menu header.</summary>
    internal string CurrentFolder => _settings.FolderPath;

    /// <summary>Saves the clipboard now and presents the outcome — an icon flash, a system sound, and
    /// (on failure) an error dialog — through the <see cref="ISaveFeedback"/> seam.</summary>
    internal void SaveNow() => _feedback.Present(SaveFeedback.For(_service.Save()));

    /// <summary>
    /// Opens the destination folder in the OS shell, creating it first so a fresh profile opens a real
    /// directory (parity with the macOS <c>makeDestinationFolder()</c>-then-open).
    /// </summary>
    internal void OpenFolder()
    {
        var folder = _settings.FolderPath;
        try
        {
            _fileSystem.CreateDirectory(folder);
        }
        catch (Exception)
        {
            // Best-effort: a failed create must not block opening (parity with the macOS
            // (try? makeDestinationFolder()) ?? folderURL fallback). Explorer surfaces a missing path.
        }

        _folderLauncher.Open(folder);
    }

    /// <summary>
    /// Asks the user for a folder and persists it — but only on a real selection, so cancelling
    /// leaves the current folder untouched (an empty path would break folder defaulting).
    /// </summary>
    internal void ChangeFolder()
    {
        var picked = _folderPicker.PickFolder(_settings.FolderPath);
        if (!string.IsNullOrWhiteSpace(picked))
        {
            _settings.LogFolderPath = picked;
        }
    }
}
