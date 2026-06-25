using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Tests the pure SaveResult→presentation policy <see cref="SaveFeedback.For"/> — the WinForms-free
/// mapping the rendering adapter (<see cref="WinFormsSaveFeedback"/>) only renders (ADR-0010). Pins the
/// macOS-parity contract (<c>AppDelegate.performSave</c>): success → check icon + success sound, no
/// dialog; empty clipboard → warning icon + the critical sound, no dialog; failure → X icon + the
/// critical sound + the failure message. No fake needed — <c>For</c> is a pure function.
/// </summary>
public class SaveFeedbackTests
{
    [Fact]
    public void For_Saved_MapsToSuccessIconAsteriskSoundNoDialog()
    {
        var feedback = SaveFeedback.For(new SaveResult.Saved(@"C:\logs\clip.txt"));

        Assert.Equal(SaveFeedback.IconKind.Success, feedback.Icon);
        Assert.Equal(SaveFeedback.SoundKind.Success, feedback.Sound);
        Assert.Null(feedback.DialogMessage);
    }

    [Fact]
    public void For_NothingToSave_MapsToWarningIconCriticalSoundNoDialog()
    {
        var feedback = SaveFeedback.For(new SaveResult.NothingToSave());

        Assert.Equal(SaveFeedback.IconKind.Warning, feedback.Icon);
        Assert.Equal(SaveFeedback.SoundKind.Critical, feedback.Sound);
        Assert.Null(feedback.DialogMessage);
    }

    [Fact]
    public void For_Failure_MapsToFailureIconCriticalSoundWithMessage()
    {
        var feedback = SaveFeedback.For(new SaveResult.Failure("boom"));

        Assert.Equal(SaveFeedback.IconKind.Failure, feedback.Icon);
        Assert.Equal(SaveFeedback.SoundKind.Critical, feedback.Sound);
        Assert.Equal("boom", feedback.DialogMessage);
    }
}
