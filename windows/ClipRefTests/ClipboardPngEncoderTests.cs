using System.Drawing;
using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Tests the DIB-to-PNG re-encode in <see cref="WinFormsClipboardReader.EncodePng"/> with an
/// in-memory <see cref="Bitmap"/> — no clipboard and no STA thread, so it runs as an ordinary
/// unit test. The literal <c>Clipboard.*</c> reads in <see cref="WinFormsClipboardReader.Read"/>
/// stay OS/STA-bound and are not covered here (like JsonSettingsStore's production path).
/// </summary>
public class ClipboardPngEncoderTests
{
    [Fact]
    public void EncodePngProducesPngSignature()
    {
        using var bitmap = new Bitmap(1, 1);

        var bytes = WinFormsClipboardReader.EncodePng(bitmap);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes[..4]);
    }

    [Fact]
    public void EncodePngRoundTripsToReadableImage()
    {
        using var bitmap = new Bitmap(3, 2);

        var bytes = WinFormsClipboardReader.EncodePng(bitmap);

        using var stream = new MemoryStream(bytes);
        using var loaded = Image.FromStream(stream);
        Assert.Equal(3, loaded.Width);
        Assert.Equal(2, loaded.Height);
    }
}
