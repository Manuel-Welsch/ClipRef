using ClipRef;

namespace ClipRefTests;

/// <summary>
/// In-memory <see cref="IAutoStart"/> test double — holds the launch-at-login state in a field
/// (instead of the registry), counts the write calls so the first-run guard can be asserted, and can
/// be told to throw on a write to exercise the toggle-failure (error-dialog) path.
/// </summary>
internal sealed class FakeAutoStart : IAutoStart
{
    private bool _enabled;

    internal FakeAutoStart(bool enabled = false)
    {
        _enabled = enabled;
    }

    internal int EnableCallCount { get; private set; }

    internal int DisableCallCount { get; private set; }

    /// <summary>When true, <see cref="Enable"/>/<see cref="Disable"/> throw — simulating a denied write.</summary>
    internal bool ThrowOnWrite { get; set; }

    public bool IsEnabled => _enabled;

    public void Enable()
    {
        if (ThrowOnWrite)
        {
            throw new InvalidOperationException("Simulated registry write failure.");
        }

        EnableCallCount++;
        _enabled = true;
    }

    public void Disable()
    {
        if (ThrowOnWrite)
        {
            throw new InvalidOperationException("Simulated registry write failure.");
        }

        DisableCallCount++;
        _enabled = false;
    }
}
