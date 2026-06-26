using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Integration coverage for the production <see cref="NtfsFileTagger"/> over a real NTFS Alternate
/// Data Stream — the real-I/O path ADR-0007 deferred from the unit suite (which fakes
/// <see cref="IFileTagger"/> in <see cref="FileTaggerContractTests"/>). Mirrors the macOS
/// <c>testSavedDateRoundTripsThroughXattr</c>: a tagged file reads its save instant back, an untagged
/// file reads back <c>null</c> ("not one of ours"), and an unparseable stream value also reads back
/// <c>null</c> (the <c>Decode</c> failure branch the in-memory double can't reach).
/// <c>[Trait Category=Integration]</c> so the upcoming CI job can include/exclude it; skipped (not
/// failed) on a volume without ADS support.
/// </summary>
[Trait("Category", "Integration")]
public class NtfsFileTaggerIntegrationTests
{
    [Fact]
    public void TaggedFile_SavedDateRoundTripsTheInstant()
    {
        using var dir = new IntegrationTestSupport.TempDir();
        if (!IntegrationTestSupport.AdsSupported(dir.Path)) return; // ADS unsupported here → nothing to verify

        var file = dir.File("report.pdf");
        File.WriteAllText(file, "x");
        // Mirrors the macOS fixture Date(timeIntervalSince1970: 1_700_000_000), as an absolute UTC instant.
        var when = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000).UtcDateTime;

        var tagger = new NtfsFileTagger();
        tagger.TagAsSaved(file, when);
        var readBack = tagger.SavedDate(file);

        Assert.NotNull(readBack);
        Assert.True(
            Math.Abs((readBack!.Value - when).TotalSeconds) < 0.0005,
            $"round-tripped instant {readBack} differs from {when} by more than the epoch-seconds tolerance");
    }

    [Fact]
    public void UntaggedFile_SavedDateIsNull()
    {
        using var dir = new IntegrationTestSupport.TempDir();
        if (!IntegrationTestSupport.AdsSupported(dir.Path)) return; // ADS unsupported here → nothing to verify

        var file = dir.File("user-keepsake.txt");
        File.WriteAllText(file, "x"); // written, never tagged → not one of ours

        Assert.Null(new NtfsFileTagger().SavedDate(file));
    }

    [Fact]
    public void UnparseableTagValue_SavedDateIsNull()
    {
        using var dir = new IntegrationTestSupport.TempDir();
        if (!IntegrationTestSupport.AdsSupported(dir.Path)) return; // ADS unsupported here → nothing to verify

        var file = dir.File("corrupt.txt");
        File.WriteAllText(file, "x");
        // Write a non-numeric value straight into our ownership stream to hit NtfsFileTagger.Decode's
        // parse-failure path — the file looks tagged but the value is garbage, so it stays "not ours".
        File.WriteAllText(file + ":" + ClipboardSaver.Const.OwnerStream, "not-a-number");

        Assert.Null(new NtfsFileTagger().SavedDate(file));
    }
}
