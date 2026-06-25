namespace ClipRef;

/// <summary>
/// Orchestrates the save action: reads the clipboard, classifies it via
/// <see cref="ClipboardSaver.Decide(ClipboardSnapshot)"/>, writes the payload to a uniquely-named
/// file in the destination folder, and puts an <c>@&lt;path&gt;</c> reference back on the clipboard.
/// Ports the write half of the macOS <c>saveClipboard</c> (the <c>write</c> helper), minus the
/// ownership tag and prune, which are later port items. All disk and clipboard access goes through
/// the injected seams, so the flow is unit-testable; the clock makes the timestamped name deterministic.
/// </summary>
internal sealed class ClipboardSaveService
{
    private readonly IClipboardReader _reader;
    private readonly IClipboardWriter _clipboardWriter;
    private readonly IFileSystem _fileSystem;
    private readonly Settings _settings;
    private readonly Func<DateTime> _clock;

    internal ClipboardSaveService(
        IClipboardReader reader,
        IClipboardWriter clipboardWriter,
        IFileSystem fileSystem,
        Settings settings,
        Func<DateTime> clock)
    {
        _reader = reader;
        _clipboardWriter = clipboardWriter;
        _fileSystem = fileSystem;
        _settings = settings;
        _clock = clock;
    }

    /// <summary>
    /// Reads the clipboard once, decides what to save, writes it, and replaces the clipboard with an
    /// <c>@&lt;path&gt;</c> reference. A copied file that is a folder or too large fails and aborts
    /// (no fall-through); an empty or self-referential clipboard is a no-op.
    /// </summary>
    internal SaveResult Save()
    {
        var snapshot = _reader.Read();
        return ClipboardSaver.Decide(snapshot) switch
        {
            SaveDecision.CopyFile copyFile => CopyFile(copyFile.Path),
            SaveDecision.SaveText saveText => Write(
                folder => ClipboardSaver.UniqueGeneratedPath(folder, ClipboardSaver.Const.TextExtension, _clock(), _fileSystem.Exists),
                destination => _fileSystem.WriteAllText(destination, saveText.Text)),
            SaveDecision.SaveImage => SaveImage(snapshot),
            _ => new SaveResult.NothingToSave(),
        };
    }

    private SaveResult CopyFile(string source)
    {
        if (!ClipboardSaver.IsCopyableFile(source, _fileSystem.GetAttributes))
        {
            return new SaveResult.Failure(
                $"ClipRef saves files, not folders — \"{Path.GetFileName(source)}\" is a folder. Copy a file instead.");
        }

        if (!ClipboardSaver.FitsSizeLimit(source, _fileSystem.GetSize))
        {
            return new SaveResult.Failure(
                $"\"{Path.GetFileName(source)}\" is too large to copy (ClipRef's limit is 100 MB). Copy a smaller file.");
        }

        return Write(
            folder => ClipboardSaver.UniquePreferredPath(folder, Path.GetFileName(source), _fileSystem.Exists),
            destination => _fileSystem.CopyFile(source, destination));
    }

    private SaveResult SaveImage(ClipboardSnapshot snapshot)
    {
        if (snapshot.ImagePng is null)
        {
            return new SaveResult.NothingToSave();
        }

        var imagePng = snapshot.ImagePng;
        return Write(
            folder => ClipboardSaver.UniqueGeneratedPath(folder, ClipboardSaver.Const.ImageExtension, _clock(), _fileSystem.Exists),
            destination => _fileSystem.WriteAllBytes(destination, imagePng));
    }

    /// <summary>
    /// The shared write path: create the destination folder, pick the unique destination via
    /// <paramref name="destinationFor"/>, write it via <paramref name="body"/>, and swap the clipboard
    /// for an <c>@&lt;path&gt;</c> reference. The reference is the path verbatim — no quoting or
    /// escaping, even for paths with spaces, matching the macOS original.
    /// </summary>
    private SaveResult Write(Func<string, string> destinationFor, Action<string> body)
    {
        var folder = _settings.FolderPath;
        try
        {
            _fileSystem.CreateDirectory(folder);
        }
        catch (Exception exception)
        {
            return new SaveResult.Failure("Could not create folder:\n" + exception.Message);
        }

        var destination = destinationFor(folder);
        try
        {
            body(destination);
        }
        catch (Exception exception)
        {
            return new SaveResult.Failure("Could not write file:\n" + exception.Message);
        }

        _clipboardWriter.SetText("@" + destination);
        return new SaveResult.Saved(destination);
    }
}
