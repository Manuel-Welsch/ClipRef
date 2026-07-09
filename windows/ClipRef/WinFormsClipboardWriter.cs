namespace ClipRef;

/// <summary>
/// The production <see cref="IClipboardWriter"/>, writing the real OS clipboard via WinForms.
/// <see cref="Clipboard.SetText"/> replaces the contents and is STA/OS-bound, so it is verified
/// manually rather than unit-tested, like <see cref="WinFormsClipboardReader"/>. It throws on a
/// null/empty argument — the save orchestrator only ever passes a non-empty <c>"@…"</c> string.
/// </summary>
internal sealed class WinFormsClipboardWriter : IClipboardWriter
{
    public void SetText(string text) => Clipboard.SetText(text);
}
