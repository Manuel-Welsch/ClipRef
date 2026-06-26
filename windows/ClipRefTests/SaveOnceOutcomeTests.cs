using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Tests the pure SaveResult→console mapping <see cref="SaveOnceOutcome.For"/> — the WinForms-free,
/// console-free contract the headless runner (<see cref="SaveOnceMode.Run"/>) only renders (ADR-0008/0010).
/// Pins the macOS <c>main.swift</c> parity: Saved → exit 0, the bare path on stdout; NothingToSave → exit
/// 2, the fixed message on stderr; Failure → exit 1, the message on stderr (note the macOS exit-code
/// quirk: NothingToSave is 2, Failure is 1). No fake needed — <c>For</c> is a pure function.
/// </summary>
public class SaveOnceOutcomeTests
{
    [Fact]
    public void For_Saved_MapsToExitZeroPathOnStdout()
    {
        var outcome = SaveOnceOutcome.For(new SaveResult.Saved(@"C:\logs\x.txt"));

        Assert.Equal(0, outcome.ExitCode);
        Assert.Equal(@"C:\logs\x.txt", outcome.Text);
        Assert.False(outcome.ToStandardError);
    }

    [Fact]
    public void For_NothingToSave_MapsToExitTwoMessageOnStderr()
    {
        var outcome = SaveOnceOutcome.For(new SaveResult.NothingToSave());

        Assert.Equal(2, outcome.ExitCode);
        Assert.Equal("Clipboard has no text or image to save", outcome.Text);
        Assert.True(outcome.ToStandardError);
    }

    [Fact]
    public void For_Failure_MapsToExitOneMessageOnStderr()
    {
        var outcome = SaveOnceOutcome.For(new SaveResult.Failure("boom"));

        Assert.Equal(1, outcome.ExitCode);
        Assert.Equal("boom", outcome.Text);
        Assert.True(outcome.ToStandardError);
    }

    [Fact]
    public void For_Saved_PreservesExactPath_WithSpaces()
    {
        var outcome = SaveOnceOutcome.For(new SaveResult.Saved(@"C:\my logs\a b.txt"));

        Assert.Equal(@"C:\my logs\a b.txt", outcome.Text);
    }
}
