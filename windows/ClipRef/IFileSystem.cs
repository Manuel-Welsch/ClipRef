namespace ClipRef;

/// <summary>
/// The seam through which the save flow touches the filesystem, so the orchestration
/// (precedence, unique naming, guard-abort, @-format) stays unit-testable without a real disk.
/// <see cref="GetAttributes"/> and <see cref="GetSize"/> return <c>null</c> on a missing or
/// unreadable path, matching the <c>Func&lt;string, FileAttributes?&gt;</c> /
/// <c>Func&lt;string, long?&gt;</c> contracts that <see cref="ClipboardSaver.IsCopyableFile"/>
/// and <see cref="ClipboardSaver.FitsSizeLimit"/> already expect. The production implementation
/// is <see cref="FileSystem"/>; tests use an in-memory double. Mirrors the
/// <see cref="ISettingsStore"/> and <see cref="IClipboardReader"/> seams.
/// <see cref="EnumerateFiles"/> (top-level, non-recursive; empty on a missing/unreadable folder,
/// like the <c>null</c>-returning readers) and <see cref="DeleteFile"/> back the prune sweep — the
/// orchestrator isolates a single delete failure, so this seam lets it throw.
/// </summary>
internal interface IFileSystem
{
    bool Exists(string path);

    void CreateDirectory(string path);

    void CopyFile(string source, string destination);

    void WriteAllText(string path, string text);

    void WriteAllBytes(string path, byte[] bytes);

    FileAttributes? GetAttributes(string path);

    long? GetSize(string path);

    IEnumerable<string> EnumerateFiles(string folder);

    void DeleteFile(string path);
}
