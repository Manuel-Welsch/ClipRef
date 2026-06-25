namespace ClipRef;

/// <summary>
/// The production <see cref="IFileSystem"/>, mapping 1:1 to <see cref="File"/> and
/// <see cref="Directory"/>. Thin and STA-free but disk-bound, so it is verified manually / at
/// integration (like <see cref="JsonSettingsStore"/>'s production path) rather than unit-tested.
/// <see cref="GetAttributes"/> and <see cref="GetSize"/> swallow the expected missing/unreadable
/// failures and return <c>null</c>, mirroring the macOS <c>try?</c> best-effort reads;
/// <see cref="EnumerateFiles"/> likewise swallows to an empty sequence. <see cref="DeleteFile"/> is a
/// thin <see cref="File.Delete"/> that lets failures throw — the prune sweep isolates a single bad
/// file itself, keeping the per-file best-effort decision in one place.
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

    public IEnumerable<string> EnumerateFiles(string folder)
    {
        try
        {
            // Materialize eagerly so a missing/unreadable folder is swallowed here (parity with
            // Swift's `guard let entries = try? contentsOfDirectory ... else { return }`), not lazily
            // mid-iteration. Top-level only — Directory.EnumerateFiles never recurses by default.
            return Directory.EnumerateFiles(folder).ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return Array.Empty<string>();
        }
    }

    public void DeleteFile(string path) => File.Delete(path);
}
