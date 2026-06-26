using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Tests the WinForms-free launch-at-login controller <see cref="LoginItem"/> — the unit-tested
/// boundary of the otherwise integration-only registry wiring (ADR-0011). It drives the three members
/// over a <see cref="FakeAutoStart"/> and an in-memory <see cref="Settings"/>, with no
/// <c>Microsoft.Win32.Registry</c> and no <see cref="System.Windows.Forms.NotifyIcon"/>. These pin the
/// macOS-parity contract of <c>ClipRef/LoginItem.swift</c>: the toggle direction, that a write failure
/// propagates (so the host can show a dialog), and the first-run guard — including the crux that a
/// later manual disable stays disabled.
/// </summary>
public class LoginItemTests
{
    private static Settings SettingsWith(params (string Key, string Value)[] seed) =>
        new(new InMemorySettingsStore(seed));

    [Fact]
    public void IsEnabled_ReflectsRegistry()
    {
        Assert.True(new LoginItem(new FakeAutoStart(enabled: true), SettingsWith()).IsEnabled);
        Assert.False(new LoginItem(new FakeAutoStart(enabled: false), SettingsWith()).IsEnabled);
    }

    [Fact]
    public void Toggle_WhenDisabled_Enables()
    {
        var autoStart = new FakeAutoStart(enabled: false);
        var loginItem = new LoginItem(autoStart, SettingsWith());

        loginItem.Toggle();

        Assert.True(autoStart.IsEnabled);
        Assert.Equal(1, autoStart.EnableCallCount);
    }

    [Fact]
    public void Toggle_WhenEnabled_Disables()
    {
        var autoStart = new FakeAutoStart(enabled: true);
        var loginItem = new LoginItem(autoStart, SettingsWith());

        loginItem.Toggle();

        Assert.False(autoStart.IsEnabled);
        Assert.Equal(1, autoStart.DisableCallCount);
    }

    [Fact]
    public void Toggle_PropagatesWriteFailure()
    {
        var autoStart = new FakeAutoStart(enabled: false) { ThrowOnWrite = true };
        var loginItem = new LoginItem(autoStart, SettingsWith());

        // The write failure surfaces so the host can present an error dialog (parity with macOS toggle()).
        Assert.Throws<InvalidOperationException>(() => loginItem.Toggle());
    }

    [Fact]
    public void EnableOnFirstRun_FreshProfile_EnablesAndSetsFlag()
    {
        var autoStart = new FakeAutoStart(enabled: false);
        var settings = SettingsWith(); // didConfigureLoginItem defaults to false
        var loginItem = new LoginItem(autoStart, settings);

        loginItem.EnableOnFirstRun();

        Assert.True(autoStart.IsEnabled);            // first run opts the user in
        Assert.True(settings.DidConfigureLoginItem); // and records that it configured the item
    }

    [Fact]
    public void EnableOnFirstRun_AlreadyEnabled_SetsFlagWithoutReEnabling()
    {
        var autoStart = new FakeAutoStart(enabled: true);
        var settings = SettingsWith();
        var loginItem = new LoginItem(autoStart, settings);

        loginItem.EnableOnFirstRun();

        Assert.True(settings.DidConfigureLoginItem);
        Assert.True(autoStart.IsEnabled);
        Assert.Equal(0, autoStart.EnableCallCount); // already enabled → the guard skips a redundant write
    }

    [Fact]
    public void EnableOnFirstRun_AlreadyConfigured_StaysDisabled()
    {
        var autoStart = new FakeAutoStart(enabled: false);
        var settings = SettingsWith(("didConfigureLoginItem", "true")); // a prior run already configured it
        var loginItem = new LoginItem(autoStart, settings);

        loginItem.EnableOnFirstRun();

        Assert.False(autoStart.IsEnabled);          // a manual disable stays disabled (the crux of the parity)
        Assert.Equal(0, autoStart.EnableCallCount);
    }
}
