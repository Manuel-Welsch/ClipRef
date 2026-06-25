namespace ClipRef;

/// <summary>
/// Presents the outcome of a save to the user. The WinForms-free <see cref="TrayActions"/> computes a
/// <see cref="SaveFeedback"/> from the <see cref="SaveResult"/> and hands it here; the production
/// <see cref="WinFormsSaveFeedback"/> renders it (icon flash, system sound, error dialog) and is the
/// untested integration shell, while the mapping stays unit-testable behind this seam (ADR-0010).
/// </summary>
internal interface ISaveFeedback
{
    void Present(SaveFeedback feedback);
}
