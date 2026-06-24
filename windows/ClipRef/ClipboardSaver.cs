namespace ClipRef;

/// <summary>
/// Decides what to do with the clipboard and recognizes ClipRef's own @-path references.
/// Pure and side-effect-free, so it is fully unit-testable. Mirrors the macOS
/// <c>ClipboardSaver</c>; it currently holds only static members by design and will grow
/// <c>uniqueURL</c>, the settings store, and the clipboard-saving action (with instance
/// state) in later port items.
/// </summary>
internal sealed class ClipboardSaver
{
    /// <summary>
    /// What the save action should do, decided purely from what's on the clipboard: a real
    /// file wins (copied as-is), then non-empty text (unless it's one of our references),
    /// then an image; otherwise nothing.
    /// </summary>
    internal static SaveDecision Decide(string? filePath, string? text, bool hasImage)
    {
        if (filePath is not null)
        {
            return new SaveDecision.CopyFile(filePath);
        }

        if (!string.IsNullOrEmpty(text))
        {
            return LooksLikeReference(text) ? new SaveDecision.Ignore() : new SaveDecision.SaveText(text);
        }

        return hasImage ? new SaveDecision.SaveImage() : new SaveDecision.Ignore();
    }

    /// <summary>
    /// True when <paramref name="text"/> already holds one of our @-path references: a single
    /// token starting with '@' followed by a Windows absolute path. Used to skip re-saving a
    /// reference that the previous click just put on the clipboard.
    /// </summary>
    internal static bool LooksLikeReference(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith('@') || trimmed.Any(char.IsWhiteSpace))
        {
            return false;
        }

        return IsWindowsAbsolutePath(trimmed.AsSpan(1));
    }

    /// <summary>
    /// True for a Windows absolute path: a drive root (<c>C:\</c>), or a leading backslash
    /// covering UNC (<c>\\server\share</c>) and rooted (<c>\folder</c>) paths. Forward-slash
    /// and home (<c>~</c>) paths are intentionally not recognized in the Windows port.
    /// </summary>
    private static bool IsWindowsAbsolutePath(ReadOnlySpan<char> path)
    {
        if (path.StartsWith("\\"))
        {
            return true;
        }

        return path.Length >= 3
            && char.IsAsciiLetter(path[0])
            && path[1] == ':'
            && path[2] == '\\';
    }
}
