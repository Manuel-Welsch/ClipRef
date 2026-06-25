using ClipRef;

namespace ClipRefTests;

/// <summary>
/// In-memory <see cref="IFileSystem"/> test double — models the filesystem with dictionaries
/// so the save orchestration runs without touching the real disk. Pre-seed existing paths with
/// <see cref="SeedExisting"/> to force unique-name collisions, and attributes/sizes to drive the
/// copy guards; the throw hooks model a folder-create or write failure. Mirrors
/// <see cref="InMemorySettingsStore"/>.
/// </summary>
internal sealed class InMemoryFileSystem : IFileSystem
{
    private readonly HashSet<string> _existing = new();
    private readonly HashSet<string> _createdDirectories = new();
    private readonly HashSet<string> _copiedTo = new();
    private readonly Dictionary<string, string> _texts = new();
    private readonly Dictionary<string, byte[]> _bytes = new();
    private readonly Dictionary<string, FileAttributes> _attributes = new();
    private readonly Dictionary<string, long> _sizes = new();

    internal bool ThrowOnCreateDirectory { get; set; }

    internal bool ThrowOnWrite { get; set; }

    internal IReadOnlyCollection<string> CreatedDirectories => _createdDirectories;

    /// <summary>True when nothing has been written — no text, no bytes, no copied file.</summary>
    internal bool WroteNothing => _texts.Count == 0 && _bytes.Count == 0 && _copiedTo.Count == 0;

    /// <summary>Marks <paramref name="path"/> as already present, to force a naming collision.</summary>
    internal void SeedExisting(string path) => _existing.Add(path);

    internal void SetAttributes(string path, FileAttributes attributes) => _attributes[path] = attributes;

    internal void SetSize(string path, long size) => _sizes[path] = size;

    internal string? TextAt(string path) => _texts.TryGetValue(path, out var text) ? text : null;

    internal byte[]? BytesAt(string path) => _bytes.TryGetValue(path, out var bytes) ? bytes : null;

    internal bool CopiedTo(string path) => _copiedTo.Contains(path);

    public bool Exists(string path) =>
        _existing.Contains(path) || _texts.ContainsKey(path) || _bytes.ContainsKey(path) || _copiedTo.Contains(path);

    public void CreateDirectory(string path)
    {
        if (ThrowOnCreateDirectory)
        {
            throw new IOException("create-directory failed");
        }

        _createdDirectories.Add(path);
    }

    public void CopyFile(string source, string destination)
    {
        if (ThrowOnWrite)
        {
            throw new IOException("copy failed");
        }

        _copiedTo.Add(destination);
    }

    public void WriteAllText(string path, string text)
    {
        if (ThrowOnWrite)
        {
            throw new IOException("write failed");
        }

        _texts[path] = text;
    }

    public void WriteAllBytes(string path, byte[] bytes)
    {
        if (ThrowOnWrite)
        {
            throw new IOException("write failed");
        }

        _bytes[path] = bytes;
    }

    public FileAttributes? GetAttributes(string path) =>
        _attributes.TryGetValue(path, out var attributes) ? attributes : null;

    public long? GetSize(string path) =>
        _sizes.TryGetValue(path, out var size) ? size : null;
}
