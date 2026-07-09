using System.Globalization;

namespace ClipRef;

/// <summary>
/// The production <see cref="IFileTagger"/> over NTFS Alternate Data Streams, addressed via the
/// <c>"&lt;path&gt;:&lt;streamName&gt;"</c> syntax that <see cref="File"/> already understands.
/// Thin and OS/NTFS-bound, so it is verified manually / at integration (like <see cref="FileSystem"/>
/// and <see cref="WinFormsClipboardWriter"/>), not unit-tested. The tag value is the save instant as
/// epoch seconds — absolute via <see cref="DateTimeOffset"/>/UTC and invariant-culture formatted, so
/// the round-trip is locale-independent and stable. Writing is best-effort: expected ADS failures are
/// swallowed, mirroring the macOS original discarding <c>setxattr</c>'s result. ADS is NTFS-only — on
/// a non-NTFS volume the stream silently doesn't persist and <see cref="SavedDate"/> returns
/// <c>null</c>, a safe degradation that simply leaves the file untouched by prune.
/// </summary>
internal sealed class NtfsFileTagger : IFileTagger
{
    public void TagAsSaved(string path, DateTime date)
    {
        try
        {
            File.WriteAllText(StreamPath(path), Encode(date));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            // Best-effort: a failed tag must not deny the user their save (parity with setxattr).
        }
    }

    public DateTime? SavedDate(string path)
    {
        try
        {
            return Decode(File.ReadAllText(StreamPath(path)));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    private static string StreamPath(string path) => path + ":" + ClipboardSaver.Const.OwnerStream;

    private static string Encode(DateTime date)
    {
        var seconds = (new DateTimeOffset(date.ToUniversalTime()) - DateTimeOffset.UnixEpoch).TotalSeconds;
        return seconds.ToString("R", CultureInfo.InvariantCulture);
    }

    private static DateTime? Decode(string value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return DateTimeOffset.UnixEpoch.AddSeconds(seconds).UtcDateTime;
        }

        return null;
    }
}
