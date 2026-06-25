using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Pins the <see cref="IFileTagger"/> seam contract that prune (item 4) will trust, exercised
/// through the in-memory double: a tagged file reads its instant back, and a file that was never
/// tagged reads back as <c>null</c> ("not one of ours"). The production <see cref="NtfsFileTagger"/>
/// real-ADS round-trip and the epoch-seconds parse (incl. an unparseable value → <c>null</c>) are
/// integration-only, like <see cref="FileSystem"/>'s real I/O.
/// </summary>
public class FileTaggerContractTests
{
    [Fact]
    public void RoundTrip_TagThenReadReturnsInstant()
    {
        var tagger = new InMemoryFileTagger();
        var when = new DateTime(2026, 6, 25, 13, 30, 45, DateTimeKind.Utc);

        tagger.TagAsSaved(@"C:\logs\clip.txt", when);
        var read = tagger.SavedDate(@"C:\logs\clip.txt");

        Assert.NotNull(read);
        Assert.Equal(when, read!.Value);
    }

    [Fact]
    public void NoTag_ReturnsNull()
    {
        var tagger = new InMemoryFileTagger();

        Assert.Null(tagger.SavedDate(@"C:\logs\untagged.txt"));
    }
}
