using ClipRef;

namespace ClipRefTests;

/// <summary>
/// In-memory <see cref="IFileSystem"/> test double — models the filesystem with dictionaries
/// so the save orchestration runs without touching the real disk. Pre-seed existing paths with
/// <see cref="SeedExisting"/> to force unique-name collisions (and, for prune, to stand in for a
/// file already in the folder), and attributes/sizes to drive the copy guards; the throw hooks
/// model a folder-create, write, or single-file delete failure. <see cref="EnumerateFiles"/> and
/// <see cref="DeleteFile"/> back the prune sweep, with <see cref="Deleted"/> recording removals so
/// a test can assert what survived. Mirrors <see cref="InMemorySettingsStore"/>.
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
    private readonly HashSet<string> _deleted = new();
    private readonly HashSet<string> _failDeletes = new();

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

    /// <summary>Makes <see cref="DeleteFile"/> throw for <paramref name="path"/>, to prove the prune
    /// sweep isolates a single locked/permission-denied file and continues with the rest.</summary>
    internal void FailDeleteOf(string path) => _failDeletes.Add(path);

    /// <summary>True once <paramref name="path"/> has been removed by <see cref="DeleteFile"/>.</summary>
    internal bool Deleted(string path) => _deleted.Contains(path);

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

    /// <summary>
    /// Top-level files present in <paramref name="folder"/> — every seeded, written, or copied path
    /// (minus those already deleted) whose directory equals the folder. Non-recursive and
    /// folder-scoped, so a file in another directory (e.g. a copy source) is never returned. An
    /// unknown/empty folder yields an empty sequence, modelling prune's silent no-op.
    /// </summary>
    public IEnumerable<string> EnumerateFiles(string folder)
    {
        var present = new HashSet<string>(_existing);
        present.UnionWith(_texts.Keys);
        present.UnionWith(_bytes.Keys);
        present.UnionWith(_copiedTo);
        present.ExceptWith(_deleted);
        return present
            .Where(path => string.Equals(Path.GetDirectoryName(path), folder, StringComparison.Ordinal))
            .ToArray();
    }

    /// <summary>
    /// Removes <paramref name="path"/> from every collection so <see cref="Exists"/> reads false,
    /// and records it for <see cref="Deleted"/>. Throws when <see cref="FailDeleteOf"/> registered the
    /// path, modelling a locked/permission-denied file the prune sweep must skip over.
    /// </summary>
    public void DeleteFile(string path)
    {
        if (_failDeletes.Contains(path))
        {
            throw new IOException("delete failed");
        }

        _existing.Remove(path);
        _texts.Remove(path);
        _bytes.Remove(path);
        _copiedTo.Remove(path);
        _deleted.Add(path);
    }
}
