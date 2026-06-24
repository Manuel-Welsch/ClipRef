using System.Drawing;
using System.Drawing.Imaging;

namespace ClipRef;

/// <summary>
/// The production <see cref="IClipboardReader"/>, reading the real OS clipboard via WinForms.
/// One <see cref="Read"/> takes the first copied file, the text, and any image (re-encoded to
/// PNG), mirroring the eager reads in the macOS <c>saveClipboard</c> (<c>firstFileURL</c> and
/// <c>pngData</c>). The literal <see cref="Clipboard"/> calls are STA/OS-bound and verified
/// manually; only <see cref="EncodePng"/> is unit-tested.
/// </summary>
internal sealed class WinFormsClipboardReader : IClipboardReader
{
    public ClipboardSnapshot Read()
    {
        return new ClipboardSnapshot(FirstFile(), NormalizeText(Clipboard.GetText()), ReadImageAsPng());
    }

    /// <summary>The first file copied to the clipboard, or <c>null</c> — mirrors the macOS
    /// <c>firstFileURL</c> taking only the first of the file URLs.</summary>
    private static string? FirstFile()
    {
        var files = Clipboard.GetFileDropList();
        return files.Count > 0 ? files[0] : null;
    }

    /// <summary>Collapses absent-or-empty clipboard text to <c>null</c> so the snapshot agrees
    /// with the empty-text branch of <see cref="ClipboardSaver.Decide(string?, string?, bool)"/>.</summary>
    private static string? NormalizeText(string? text)
    {
        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>The clipboard image re-encoded to PNG, or <c>null</c> when there is no image —
    /// mirrors the macOS <c>pngData</c> re-encoding at read time.</summary>
    private static byte[]? ReadImageAsPng()
    {
        using var image = Clipboard.GetImage();
        return image is null ? null : EncodePng(image);
    }

    /// <summary>Encodes <paramref name="image"/> as PNG bytes. Isolated from the clipboard so it
    /// can be unit-tested without an STA thread.</summary>
    internal static byte[] EncodePng(Image image)
    {
        using var stream = new MemoryStream();
        image.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
