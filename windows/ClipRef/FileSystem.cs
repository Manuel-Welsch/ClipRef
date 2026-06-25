namespace ClipRef;

/// <summary>
/// The production <see cref="IFileSystem"/>, mapping 1:1 to <see cref="File"/> and
/// <see cref="Directory"/>. Thin and STA-free but disk-bound, so it is verified manually / at
/// integration (like <see cref="JsonSettingsStore"/>'s production path) rather than unit-tested.
/// <see cref="GetAttributes"/> and <see cref="GetSize"/> swallow the expected missing/unreadable
/// failures and return <c>null</c>, mirroring the macOS <c>try?</c> best-effort reads.
/// </summary>
internal sealed class FileSystem : IFileSystem
{
    public bool Exists(string path) => File.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void CopyFile(string source, string destination) => File.Copy(source, destination);

    public void WriteAllText(string path, string text) => File.WriteAllText(path, text);

    public void WriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);

    public FileAttributes? GetAttributes(string path)
    {
        try
        {
            return File.GetAttributes(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    public long? GetSize(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }
}
