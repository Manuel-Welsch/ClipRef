using System.Globalization;

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

    /// <summary>
    /// A non-colliding path named <c>clip-&lt;timestamp&gt;.&lt;extension&gt;</c> inside
    /// <paramref name="folder"/>, used for saved text and images. <paramref name="now"/> is
    /// formatted with the invariant culture so the name is stable; <paramref name="exists"/>
    /// reports whether a candidate is already taken, appending <c>-2</c>, <c>-3</c>, … on collision.
    /// </summary>
    internal static string UniqueGeneratedPath(string folder, string extension, DateTime now, Func<string, bool> exists)
    {
        var stamp = now.ToString(Const.TimestampFormat, CultureInfo.InvariantCulture);
        return UniquePath(folder, Const.FilePrefix + stamp, "-", extension, exists);
    }

    /// <summary>
    /// A non-colliding path inside <paramref name="folder"/> that keeps
    /// <paramref name="preferredName"/> as-is (used for copied files), adding a Finder-style
    /// <c> 2</c>, <c> 3</c>, … before the extension when <paramref name="exists"/> reports a
    /// clash (<c>report 2.pdf</c>).
    /// </summary>
    internal static string UniquePreferredPath(string folder, string preferredName, Func<string, bool> exists)
    {
        var stem = Path.GetFileNameWithoutExtension(preferredName);
        var extension = Path.GetExtension(preferredName).TrimStart('.');
        return UniquePath(folder, stem, " ", extension, exists);
    }

    /// <summary>
    /// Returns the first path of the form <c>stem(.extension)</c> in <paramref name="folder"/> for
    /// which <paramref name="exists"/> is false, disambiguating a collision by inserting
    /// <paramref name="separator"/> and a counter (from 2) before the extension. An empty extension
    /// yields no trailing dot.
    /// </summary>
    private static string UniquePath(string folder, string stem, string separator, string extension, Func<string, bool> exists)
    {
        var suffix = extension.Length == 0 ? string.Empty : "." + extension;
        var candidate = Path.Combine(folder, stem + suffix);
        var counter = 2;
        while (exists(candidate))
        {
            candidate = Path.Combine(folder, $"{stem}{separator}{counter}{suffix}");
            counter++;
        }

        return candidate;
    }

    /// <summary>Naming constants mirrored from the macOS reference.</summary>
    private static class Const
    {
        internal const string FilePrefix = "clip-";

        // Dashes for the date, dots for the time (mirrors macOS screenshot names) so the two read
        // apart at a glance; '_' between. Shell- and @-reference-safe, and sortable.
        internal const string TimestampFormat = "yyyy-MM-dd'_'HH.mm.ss";
    }
}
