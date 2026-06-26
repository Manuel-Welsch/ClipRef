using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Tests the pure flag predicate <see cref="SaveOnceMode.IsRequested"/> — the headless-mode trigger.
/// Pins the locked, macOS-faithful contract: the exact, case-sensitive <c>--save-once</c> token only.
/// (The runner <see cref="SaveOnceMode.Run"/> is integration-only shell and is not unit-tested.)
/// </summary>
public class SaveOnceModeTests
{
    [Fact]
    public void IsRequested_WithExactFlag_ReturnsTrue()
    {
        Assert.True(SaveOnceMode.IsRequested(new[] { "--save-once" }));
        Assert.True(SaveOnceMode.IsRequested(new[] { "--other", "--save-once" }));
    }

    [Fact]
    public void IsRequested_WithoutFlag_ReturnsFalse()
    {
        Assert.False(SaveOnceMode.IsRequested(new[] { "--other" }));
        Assert.False(SaveOnceMode.IsRequested(Array.Empty<string>()));
    }

    [Fact]
    public void IsRequested_IsCaseSensitive_ReturnsFalseForWrongCase()
    {
        Assert.False(SaveOnceMode.IsRequested(new[] { "--Save-Once" }));
        Assert.False(SaveOnceMode.IsRequested(new[] { "--SAVE-ONCE" }));
    }

    [Fact]
    public void IsRequested_DoesNotMatchSubstringOrAltSpelling_ReturnsFalse()
    {
        Assert.False(SaveOnceMode.IsRequested(new[] { "/save-once" }));
        Assert.False(SaveOnceMode.IsRequested(new[] { "-save-once" }));
        Assert.False(SaveOnceMode.IsRequested(new[] { "--save-once-extra" }));
    }
}
