namespace ClipRef;

/// <summary>
/// The WinForms-free, console-free mapping of a <see cref="SaveResult"/> to what the headless
/// <c>--save-once</c> mode prints and exits with: an exit code, the line of text, and whether it goes to
/// standard error. Mirrors the macOS <c>main.swift</c> headless branch — Saved → exit 0, the bare path on
/// stdout; NothingToSave → exit 2, a fixed message on stderr; Failure → exit 1, the message on stderr.
/// The pure factory <see cref="For"/> holds that parity contract so it is unit-testable;
/// <see cref="SaveOnceMode"/> only renders it (ADR-0008/0010).
/// </summary>
internal sealed record SaveOnceOutcome(int ExitCode, string Text, bool ToStandardError)
{
    private const string NothingToSaveMessage = "Clipboard has no text or image to save";

    /// <summary>
    /// Maps a save outcome to its console presentation: <see cref="SaveResult.Saved"/> → exit 0 with the
    /// bare path on stdout; <see cref="SaveResult.NothingToSave"/> → exit 2 with a fixed message on
    /// stderr; <see cref="SaveResult.Failure"/> → exit 1 with the failure message on stderr. The exit-code
    /// pairing (empty = 2, failure = 1) is the macOS quirk preserved verbatim (<c>main.swift:11-17</c>).
    /// </summary>
    internal static SaveOnceOutcome For(SaveResult result) => result switch
    {
        SaveResult.Saved saved => new(0, saved.SavedPath, ToStandardError: false),
        SaveResult.NothingToSave => new(2, NothingToSaveMessage, ToStandardError: true),
        SaveResult.Failure failure => new(1, failure.Message, ToStandardError: true),
        _ => new(1, "Unknown save outcome.", ToStandardError: true), // defensive; the union (ADR-0003) is closed
    };
}
