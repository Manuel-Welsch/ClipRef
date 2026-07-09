namespace ClipRef;

/// <summary>
/// What <see cref="ClipboardSaver.Decide"/> chose to do with the clipboard, decided purely
/// from its contents: a real file wins (copied as-is), then text, then an image; ClipRef's
/// own @-path references are ignored. A closed union — only the nested cases may derive,
/// so a <c>switch</c> over them is exhaustive and instances compare by value.
/// </summary>
internal abstract record SaveDecision
{
    private protected SaveDecision()
    {
    }

    internal sealed record CopyFile(string Path) : SaveDecision;

    internal sealed record SaveText(string Text) : SaveDecision;

    internal sealed record SaveImage : SaveDecision;

    internal sealed record Ignore : SaveDecision;
}
