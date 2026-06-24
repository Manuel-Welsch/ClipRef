namespace ClipRef;

/// <summary>
/// The seam through which ClipRef reads the OS clipboard. <see cref="Read"/> performs one
/// round-trip and returns an immutable <see cref="ClipboardSnapshot"/>, so the classification
/// logic stays pure and unit-testable. The production implementation is WinForms-backed
/// (<see cref="WinFormsClipboardReader"/>); tests use an in-memory double. Mirrors the
/// <see cref="ISettingsStore"/> seam.
/// </summary>
internal interface IClipboardReader
{
    ClipboardSnapshot Read();
}
