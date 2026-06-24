namespace ClipRef;

/// <summary>
/// An immutable snapshot of one eager read of the OS clipboard: the first copied file path
/// (<c>null</c> when no file is present), the text (<c>null</c> when absent or empty), and the
/// clipboard image already re-encoded to PNG bytes (<c>null</c> when there is no image). The
/// Windows analogue of the three values the macOS <c>saveClipboard</c> reads up front
/// (<c>firstFileURL</c>, the pasteboard string, and <c>pngData</c>).
///
/// The synthesized record equality compares <see cref="ImagePng"/> by reference (a
/// <c>byte[]</c> caveat); harmless here because snapshots are never compared by value.
/// </summary>
internal sealed record ClipboardSnapshot(string? FilePath, string? Text, byte[]? ImagePng);
