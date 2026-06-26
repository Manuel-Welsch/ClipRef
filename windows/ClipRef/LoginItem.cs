namespace ClipRef;

/// <summary>
/// Launch-at-login policy for the app — the WinForms-free, unit-tested controller behind the tray's
/// "Launch at Login" menu item and the launch-time first-run opt-in. A faithful port of the macOS
/// <c>LoginItem</c> (<c>ClipRef/LoginItem.swift</c>): the OS touch-point sits behind the
/// <see cref="IAutoStart"/> seam and the first-run guard reuses <see cref="Settings.DidConfigureLoginItem"/>,
/// so all three members are exercised by xUnit over in-memory doubles (ADR-0011).
/// </summary>
internal sealed class LoginItem
{
    private readonly IAutoStart _autoStart;
    private readonly Settings _settings;

    internal LoginItem(IAutoStart autoStart, Settings settings)
    {
        _autoStart = autoStart;
        _settings = settings;
    }

    /// <summary>Whether the app is currently registered to start at login (drives the menu checkmark).</summary>
    internal bool IsEnabled => _autoStart.IsEnabled;

    /// <summary>
    /// Flips the launch-at-login state: disables it when enabled, enables it when disabled. May throw if
    /// the underlying write is denied — the host catches it to show an error dialog (parity with the
    /// macOS <c>toggle()</c> throwing).
    /// </summary>
    internal void Toggle()
    {
        if (_autoStart.IsEnabled)
        {
            _autoStart.Disable();
        }
        else
        {
            _autoStart.Enable();
        }
    }

    /// <summary>
    /// Registers the app to start at login the first time it runs (the user opted in), then never again:
    /// guarded by <see cref="Settings.DidConfigureLoginItem"/> so a later manual disable stays disabled.
    /// Parity with the macOS <c>enableOnFirstRun()</c> — runs only on the tray launch path, never in the
    /// headless <c>--save-once</c> mode.
    /// </summary>
    internal void EnableOnFirstRun()
    {
        if (_settings.DidConfigureLoginItem)
        {
            return;
        }

        _settings.DidConfigureLoginItem = true;
        if (!_autoStart.IsEnabled)
        {
            _autoStart.Enable();
        }
    }
}
