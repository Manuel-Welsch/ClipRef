namespace ClipRef;

/// <summary>
/// The seam through which ClipRef stamps each saved file with its ownership tag — an NTFS
/// Alternate Data Stream named <see cref="ClipboardSaver.Const.OwnerStream"/>
/// (<c>de.manuelwelsch.ClipRef.savedAt</c>) whose value is the save instant as epoch seconds — and
/// reads it back. <see cref="SavedDate"/> returns <c>null</c> for a file without our tag or with an
/// unparseable value: the "not one of ours, leave it alone" contract that prune (a later port item)
/// trusts. The production implementation is <see cref="NtfsFileTagger"/>; tests use an in-memory
/// double. Mirrors the <see cref="IFileSystem"/>, <see cref="IClipboardReader"/>, and
/// <see cref="IClipboardWriter"/> seams.
/// </summary>
internal interface IFileTagger
{
    void TagAsSaved(string path, DateTime date);

    DateTime? SavedDate(string path);
}
