using ClipRef;

namespace ClipRefTests;

/// <summary>
/// In-memory <see cref="IClipboardWriter"/> test double — captures the last text put on the
/// clipboard so the @-reference put-back can be asserted without touching the OS clipboard.
/// <see cref="LastText"/> is <c>null</c> until a save puts something back. Mirrors
/// <see cref="InMemoryClipboardReader"/>.
/// </summary>
internal sealed class InMemoryClipboardWriter : IClipboardWriter
{
    internal string? LastText { get; private set; }

    public void SetText(string text) => LastText = text;
}
