using ClipRef;

namespace ClipRefTests;

/// <summary>
/// In-memory <see cref="IClipboardReader"/> test double — returns a canned
/// <see cref="ClipboardSnapshot"/> so the read-then-classify path is exercised without
/// touching the OS clipboard. Mirrors <see cref="InMemorySettingsStore"/>.
/// </summary>
internal sealed class InMemoryClipboardReader : IClipboardReader
{
    private readonly ClipboardSnapshot _snapshot;

    internal InMemoryClipboardReader(ClipboardSnapshot snapshot)
    {
        _snapshot = snapshot;
    }

    public ClipboardSnapshot Read() => _snapshot;
}
