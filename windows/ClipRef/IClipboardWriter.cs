namespace ClipRef;

/// <summary>
/// The seam through which ClipRef puts the @-path reference back on the OS clipboard.
/// <see cref="SetText"/> replaces the clipboard contents, covering the macOS
/// <c>clearContents()</c> + <c>setString(...)</c> pair. The production implementation is
/// <see cref="WinFormsClipboardWriter"/>; tests use an in-memory double. Mirrors the
/// <see cref="IClipboardReader"/> seam.
/// </summary>
internal interface IClipboardWriter
{
    void SetText(string text);
}
