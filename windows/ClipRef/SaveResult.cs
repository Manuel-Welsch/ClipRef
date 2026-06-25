namespace ClipRef;

/// <summary>
/// The outcome of <see cref="ClipboardSaveService.Save"/>: <see cref="Saved"/> carries the
/// destination file path (the @-reference put on the clipboard is derived from it),
/// <see cref="Failure"/> carries a user-facing message, and <see cref="NothingToSave"/> means the
/// clipboard was empty or held one of our own references. Mirrors the macOS <c>SaveResult</c>
/// (<c>success(URL)</c> / <c>failure(String)</c> / <c>noContent</c>). A closed union — only the
/// nested cases may derive, so a <c>switch</c> over them is exhaustive and instances compare by value.
/// </summary>
internal abstract record SaveResult
{
    private protected SaveResult()
    {
    }

    internal sealed record Saved(string SavedPath) : SaveResult;

    internal sealed record Failure(string Message) : SaveResult;

    internal sealed record NothingToSave : SaveResult;
}
