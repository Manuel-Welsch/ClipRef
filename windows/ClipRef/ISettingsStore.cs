namespace ClipRef;

/// <summary>
/// A minimal string-keyed persistence seam that decouples the typed <see cref="Settings"/>
/// from its backing store, so the settings logic can be unit-tested against an in-memory
/// double. <see cref="Get"/> returns <c>null</c> when the key has never been set.
/// </summary>
internal interface ISettingsStore
{
    string? Get(string key);

    void Set(string key, string value);
}
