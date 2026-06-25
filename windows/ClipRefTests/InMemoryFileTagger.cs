using ClipRef;

namespace ClipRefTests;

/// <summary>
/// In-memory <see cref="IFileTagger"/> test double — records ownership tags in a dictionary so the
/// save orchestration and the seam contract can be exercised without a real NTFS Alternate Data
/// Stream. It stores the <see cref="DateTime"/> directly (no epoch-seconds encoding), so callers
/// get exact equality on the captured instant; a path that was never tagged reads back as
/// <c>null</c> (the "not one of ours" contract prune trusts). The <see cref="ThrowOnTag"/> hook
/// models a tag write failing, to prove the orchestrator's best-effort guarantee. Mirrors
/// <see cref="InMemoryFileSystem"/>. The production epoch-string parse (incl. unparseable → null)
/// is a real-ADS concern verified at integration, not faked here.
/// </summary>
internal sealed class InMemoryFileTagger : IFileTagger
{
    private readonly Dictionary<string, DateTime> _tags = new();

    internal bool ThrowOnTag { get; set; }

    /// <summary>True when nothing has been tagged.</summary>
    internal bool TaggedNothing => _tags.Count == 0;

    internal bool Tagged(string path) => _tags.ContainsKey(path);

    internal DateTime TagOf(string path) => _tags[path];

    public void TagAsSaved(string path, DateTime date)
    {
        if (ThrowOnTag)
        {
            throw new IOException("tag failed");
        }

        _tags[path] = date;
    }

    public DateTime? SavedDate(string path) => _tags.TryGetValue(path, out var date) ? date : null;
}
