using ClipRef;

namespace ClipRefTests;

/// <summary>
/// In-memory <see cref="ISettingsStore"/> test double — the unit-test seam for
/// <see cref="Settings"/>, so its defaulting, parsing, and resolution logic is exercised
/// without touching the disk. Construct with seed pairs to model already-stored values.
/// </summary>
internal sealed class InMemorySettingsStore : ISettingsStore
{
    private readonly Dictionary<string, string> _values;

    internal InMemorySettingsStore(params (string Key, string Value)[] seed)
    {
        _values = seed.ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    public string? Get(string key) => _values.TryGetValue(key, out var value) ? value : null;

    public void Set(string key, string value) => _values[key] = value;
}
