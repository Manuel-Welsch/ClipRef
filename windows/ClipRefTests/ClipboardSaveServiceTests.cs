using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Tests the write side: <see cref="ClipboardSaveService.Save"/> running read → Decide → write →
/// @-put-back over in-memory doubles (no real disk or clipboard). Pins the precedence and
/// guard-abort, the unique-naming map (original name for a copied file, <c>clip-&lt;timestamp&gt;</c>
/// for text/image) including collisions, the verbatim @-path put-back, and the folder-create /
/// write failure mapping. A fixed clock makes the timestamped name deterministic.
/// </summary>
public class ClipboardSaveServiceTests
{
    private const string Folder = @"C:\logs";

    private static readonly DateTime FixedClock = new(2026, 6, 25, 13, 30, 45);

    private static ClipboardSaveService Service(
        ClipboardSnapshot snapshot,
        InMemoryFileSystem fileSystem,
        InMemoryClipboardWriter writer)
    {
        var settings = new Settings(new InMemorySettingsStore(("logFolderPath", Folder)));
        return new ClipboardSaveService(
            new InMemoryClipboardReader(snapshot), writer, fileSystem, settings, () => FixedClock);
    }

    [Fact]
    public void TextSaved_WritesTxtAndPutsReferenceBack()
    {
        var fileSystem = new InMemoryFileSystem();
        var writer = new InMemoryClipboardWriter();
        var result = Service(new ClipboardSnapshot(null, "hello world", null), fileSystem, writer).Save();

        var expected = Path.Combine(Folder, "clip-2026-06-25_13.30.45.txt");
        Assert.Equal<SaveResult>(new SaveResult.Saved(expected), result);
        Assert.Equal("hello world", fileSystem.TextAt(expected));
        Assert.Equal("@" + expected, writer.LastText);
    }

    [Fact]
    public void ImageSaved_WritesPng()
    {
        var fileSystem = new InMemoryFileSystem();
        var writer = new InMemoryClipboardWriter();
        var image = new byte[] { 1, 2, 3 };
        var result = Service(new ClipboardSnapshot(null, null, image), fileSystem, writer).Save();

        var expected = Path.Combine(Folder, "clip-2026-06-25_13.30.45.png");
        Assert.Equal<SaveResult>(new SaveResult.Saved(expected), result);
        Assert.Equal(image, fileSystem.BytesAt(expected));
        Assert.Equal("@" + expected, writer.LastText);
    }

    [Fact]
    public void EmptyClipboard_NothingToSave()
    {
        var fileSystem = new InMemoryFileSystem();
        var writer = new InMemoryClipboardWriter();
        var result = Service(new ClipboardSnapshot(null, null, null), fileSystem, writer).Save();

        Assert.Equal<SaveResult>(new SaveResult.NothingToSave(), result);
        Assert.True(fileSystem.WroteNothing);
        Assert.Null(writer.LastText);
    }

    [Fact]
    public void ReferenceText_NothingToSave()
    {
        var fileSystem = new InMemoryFileSystem();
        var writer = new InMemoryClipboardWriter();
        var result = Service(new ClipboardSnapshot(null, @"@C:\logs\clip.txt", null), fileSystem, writer).Save();

        Assert.Equal<SaveResult>(new SaveResult.NothingToSave(), result);
        Assert.True(fileSystem.WroteNothing);
        Assert.Null(writer.LastText);
    }

    [Fact]
    public void FileCopied_KeepsOriginalNameAndPutsReferenceBack()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.SetAttributes(@"C:\src\report.pdf", FileAttributes.Normal);
        fileSystem.SetSize(@"C:\src\report.pdf", 1000);
        var writer = new InMemoryClipboardWriter();
        var result = Service(new ClipboardSnapshot(@"C:\src\report.pdf", null, null), fileSystem, writer).Save();

        var expected = Path.Combine(Folder, "report.pdf");
        Assert.Equal<SaveResult>(new SaveResult.Saved(expected), result);
        Assert.True(fileSystem.CopiedTo(expected));
        Assert.Equal("@" + expected, writer.LastText);
    }

    [Fact]
    public void FileCopy_NonCopyable_FailsAndAborts()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.SetAttributes(@"C:\src\report.pdf", FileAttributes.Directory);
        var writer = new InMemoryClipboardWriter();
        var result = Service(new ClipboardSnapshot(@"C:\src\report.pdf", null, null), fileSystem, writer).Save();

        var failure = Assert.IsType<SaveResult.Failure>(result);
        Assert.Contains("report.pdf", failure.Message);
        Assert.Contains("folder", failure.Message);
        Assert.Contains("Copy a file", failure.Message);
        Assert.True(fileSystem.WroteNothing);
        Assert.Null(writer.LastText);
    }

    [Fact]
    public void FileCopy_TooLarge_FailsAndAborts()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.SetAttributes(@"C:\src\report.pdf", FileAttributes.Normal);
        fileSystem.SetSize(@"C:\src\report.pdf", 200_000_000);
        var writer = new InMemoryClipboardWriter();
        var result = Service(new ClipboardSnapshot(@"C:\src\report.pdf", null, null), fileSystem, writer).Save();

        var failure = Assert.IsType<SaveResult.Failure>(result);
        Assert.Contains("report.pdf", failure.Message);
        Assert.Contains("too large", failure.Message);
        Assert.True(fileSystem.WroteNothing);
        Assert.Null(writer.LastText);
    }

    [Fact]
    public void TextSave_CollisionGetsDashTwoSuffix()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.SeedExisting(Path.Combine(Folder, "clip-2026-06-25_13.30.45.txt"));
        var writer = new InMemoryClipboardWriter();
        var result = Service(new ClipboardSnapshot(null, "x", null), fileSystem, writer).Save();

        var expected = Path.Combine(Folder, "clip-2026-06-25_13.30.45-2.txt");
        Assert.Equal<SaveResult>(new SaveResult.Saved(expected), result);
        Assert.Equal("@" + expected, writer.LastText);
    }

    [Fact]
    public void FileCopy_CollisionGetsSpaceTwoSuffix()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.SetAttributes(@"C:\src\report.pdf", FileAttributes.Normal);
        fileSystem.SetSize(@"C:\src\report.pdf", 1000);
        fileSystem.SeedExisting(Path.Combine(Folder, "report.pdf"));
        var writer = new InMemoryClipboardWriter();
        var result = Service(new ClipboardSnapshot(@"C:\src\report.pdf", null, null), fileSystem, writer).Save();

        var expected = Path.Combine(Folder, "report 2.pdf");
        Assert.Equal<SaveResult>(new SaveResult.Saved(expected), result);
        Assert.True(fileSystem.CopiedTo(expected));
    }

    [Fact]
    public void ReferenceWithSpaces_NotQuoted()
    {
        var fileSystem = new InMemoryFileSystem();
        fileSystem.SetAttributes(@"C:\src\my report.pdf", FileAttributes.Normal);
        fileSystem.SetSize(@"C:\src\my report.pdf", 1000);
        var writer = new InMemoryClipboardWriter();
        var result = Service(new ClipboardSnapshot(@"C:\src\my report.pdf", null, null), fileSystem, writer).Save();

        var expected = Path.Combine(Folder, "my report.pdf");
        Assert.Equal<SaveResult>(new SaveResult.Saved(expected), result);
        Assert.Equal("@" + expected, writer.LastText); // verbatim, no quoting/escaping
    }

    [Fact]
    public void CreateDirectoryThrows_ReturnsFailure()
    {
        var fileSystem = new InMemoryFileSystem { ThrowOnCreateDirectory = true };
        var writer = new InMemoryClipboardWriter();
        var result = Service(new ClipboardSnapshot(null, "x", null), fileSystem, writer).Save();

        var failure = Assert.IsType<SaveResult.Failure>(result);
        Assert.StartsWith("Could not create folder", failure.Message);
        Assert.True(fileSystem.WroteNothing);
        Assert.Null(writer.LastText);
    }

    [Fact]
    public void WriteThrows_ReturnsFailure()
    {
        var fileSystem = new InMemoryFileSystem { ThrowOnWrite = true };
        var writer = new InMemoryClipboardWriter();
        var result = Service(new ClipboardSnapshot(null, "x", null), fileSystem, writer).Save();

        var failure = Assert.IsType<SaveResult.Failure>(result);
        Assert.StartsWith("Could not write file", failure.Message);
        Assert.Null(writer.LastText);
    }

    [Fact]
    public void FolderCreated_OnSuccessfulSave()
    {
        var fileSystem = new InMemoryFileSystem();
        var writer = new InMemoryClipboardWriter();
        Service(new ClipboardSnapshot(null, "x", null), fileSystem, writer).Save();

        Assert.Contains(Folder, fileSystem.CreatedDirectories);
    }
}
