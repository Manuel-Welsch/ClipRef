using ClipRef;

namespace ClipRefTests;

/// <summary>
/// In-memory <see cref="ISaveFeedback"/> test double — records every <see cref="SaveFeedback"/> it was
/// asked to present (instead of flashing the tray icon, playing a sound, or showing a dialog), so the
/// controller's result→presentation wiring can be asserted without a <see cref="System.Windows.Forms.NotifyIcon"/>
/// or a modal dialog. Mirrors the <see cref="FakeFolderLauncher"/> / <see cref="FakeFolderPicker"/> style.
/// </summary>
internal sealed class FakeSaveFeedback : ISaveFeedback
{
    private readonly List<SaveFeedback> _presented = new();

    internal IReadOnlyList<SaveFeedback> Presented => _presented;

    internal SaveFeedback? Last => _presented.Count > 0 ? _presented[^1] : null;

    public void Present(SaveFeedback feedback) => _presented.Add(feedback);
}
