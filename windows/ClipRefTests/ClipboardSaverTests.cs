using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Mirrors the macOS reference tests in ClipRefTests/ClipboardSaverTests.swift for the
/// pure decision core: <see cref="ClipboardSaver.Decide"/> and
/// <see cref="ClipboardSaver.LooksLikeReference"/>. Path literals are translated to
/// Windows form, and extra cases pin the Windows-only reference rule (drive root, UNC,
/// rooted backslash; forward-slash and ~ are intentionally not recognized).
/// </summary>
public class ClipboardSaverTests
{
    // decide(filePath:text:hasImage:)

    [Fact]
    public void TextIsSaved()
    {
        Assert.Equal<SaveDecision>(new SaveDecision.SaveText("hello"), ClipboardSaver.Decide(null, "hello", false));
    }

    [Fact]
    public void TextWinsOverImage()
    {
        Assert.Equal<SaveDecision>(new SaveDecision.SaveText("hello"), ClipboardSaver.Decide(null, "hello", true));
    }

    [Fact]
    public void ImageSavedWhenNoText()
    {
        Assert.Equal<SaveDecision>(new SaveDecision.SaveImage(), ClipboardSaver.Decide(null, null, true));
        Assert.Equal<SaveDecision>(new SaveDecision.SaveImage(), ClipboardSaver.Decide(null, "", true));
    }

    [Fact]
    public void EmptyClipboardIgnored()
    {
        Assert.Equal<SaveDecision>(new SaveDecision.Ignore(), ClipboardSaver.Decide(null, null, false));
        Assert.Equal<SaveDecision>(new SaveDecision.Ignore(), ClipboardSaver.Decide(null, "", false));
    }

    [Fact]
    public void ReferenceIgnoredEvenWithImage()
    {
        Assert.Equal<SaveDecision>(new SaveDecision.Ignore(), ClipboardSaver.Decide(null, @"@C:\Users\me\clip.txt", false));
        Assert.Equal<SaveDecision>(new SaveDecision.Ignore(), ClipboardSaver.Decide(null, @"@C:\Users\me\clip.png", true));
    }

    [Fact]
    public void FileIsCopied()
    {
        const string path = @"C:\tmp\report.pdf";
        Assert.Equal<SaveDecision>(new SaveDecision.CopyFile(path), ClipboardSaver.Decide(path, null, false));
    }

    [Fact]
    public void FileWinsOverTextAndImage()
    {
        const string path = @"C:\tmp\clip.mov";
        Assert.Equal<SaveDecision>(new SaveDecision.CopyFile(path), ClipboardSaver.Decide(path, "ignored text", true));
    }

    // looksLikeReference

    [Theory]
    // Our own @-references with a Windows absolute path → recognized.
    [InlineData(@"@C:\Users\me\clip.txt", true)]
    [InlineData(@"  @C:\Users\me\x.png  ", true)]   // surrounding whitespace is trimmed
    [InlineData(@"@\\server\share\clip.txt", true)] // UNC
    [InlineData(@"@\folder\clip.txt", true)]        // rooted backslash
    [InlineData(@"@c:\x.txt", true)]                // drive letter is case-insensitive
    // Not references.
    [InlineData("@here standup notes", false)]      // internal whitespace
    [InlineData("@username", false)]                // no path after @
    [InlineData("plain text", false)]
    [InlineData("", false)]
    // macOS cues are intentionally dropped in the Windows port.
    [InlineData("@/Users/me/clip.txt", false)]      // forward-slash not recognized
    [InlineData("@~/x.png", false)]                 // home (~) not recognized
    public void LooksLikeReferenceDetectsWindowsReferencesOnly(string text, bool expected)
    {
        Assert.Equal(expected, ClipboardSaver.LooksLikeReference(text));
    }
}
