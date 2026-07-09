namespace ClipRef;

/// <summary>
/// A WinForms-free description of how to present a save outcome: which icon to flash, which sound to
/// play, and the dialog message (when a failure should raise one). <see cref="For"/> holds the
/// macOS-parity policy mapping a <see cref="SaveResult"/> to this value, so the decision is unit-tested
/// while <see cref="WinFormsSaveFeedback"/> only renders it (ADR-0010). The icon and sound are named by
/// intent — not by WinForms types — and are independent axes, so the empty-clipboard case can keep its
/// own icon while sharing the failure sound (the macOS quirk we mirror).
/// </summary>
internal sealed record SaveFeedback(SaveFeedback.IconKind Icon, SaveFeedback.SoundKind Sound, string? DialogMessage)
{
    internal enum IconKind
    {
        Success,
        Warning,
        Failure,
    }

    internal enum SoundKind
    {
        Success,
        Critical,
    }

    /// <summary>
    /// Maps a <see cref="SaveResult"/> to its presentation (macOS <c>performSave</c> parity): a save
    /// flashes the success icon with the success sound; an empty/self-referential clipboard flashes the
    /// warning icon with the critical sound; a failure flashes the failure icon with the critical sound
    /// and carries its message to a dialog. Only the failure case raises a dialog.
    /// </summary>
    internal static SaveFeedback For(SaveResult result) => result switch
    {
        SaveResult.Saved => new(IconKind.Success, SoundKind.Success, null),
        SaveResult.NothingToSave => new(IconKind.Warning, SoundKind.Critical, null),
        SaveResult.Failure failure => new(IconKind.Failure, SoundKind.Critical, failure.Message),
        _ => new(IconKind.Warning, SoundKind.Critical, null), // defensive; the union (ADR-0003) is closed
    };
}
