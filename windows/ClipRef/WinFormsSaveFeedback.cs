using System.Media;

namespace ClipRef;

/// <summary>
/// Production <see cref="ISaveFeedback"/> — renders a <see cref="SaveFeedback"/> on the tray: it flashes
/// one of three embedded icons for ~1.1s (parity with the macOS <c>flashDuration</c>) before restoring
/// the default, plays the matching system sound, and shows a modal error dialog on failure. Integration-
/// only shell, like the other WinForms adapters (ADR-0010); the result→presentation policy it renders is
/// decided and unit-tested in <see cref="SaveFeedback.For"/>.
///
/// It must point at the host's live <see cref="NotifyIcon"/>, which exists only after the host is built,
/// so the host calls <see cref="Attach"/> once it has one. A single reusable timer means rapid saves just
/// re-arm the restore — the icon never sticks and always ends on the default.
/// </summary>
internal sealed class WinFormsSaveFeedback : ISaveFeedback, IDisposable
{
    private const int FlashDurationMs = 1100;

    private readonly Icon _successIcon;
    private readonly Icon _warningIcon;
    private readonly Icon _failureIcon;
    private readonly System.Windows.Forms.Timer _restoreTimer;

    private NotifyIcon? _trayIcon;
    private Icon? _defaultIcon;

    internal WinFormsSaveFeedback()
    {
        _successIcon = EmbeddedIcon.Load("ClipRef.success.ico");
        _warningIcon = EmbeddedIcon.Load("ClipRef.warning.ico");
        _failureIcon = EmbeddedIcon.Load("ClipRef.failure.ico");
        _restoreTimer = new System.Windows.Forms.Timer { Interval = FlashDurationMs };
        _restoreTimer.Tick += RestoreDefaultIcon;
    }

    /// <summary>Binds the adapter to the host's tray icon and its default image, so it can swap and
    /// restore the icon. Called once by the host after it builds the <see cref="NotifyIcon"/>.</summary>
    internal void Attach(NotifyIcon trayIcon, Icon defaultIcon)
    {
        _trayIcon = trayIcon;
        _defaultIcon = defaultIcon;
    }

    public void Present(SaveFeedback feedback)
    {
        FlashIcon(feedback.Icon);
        PlaySound(feedback.Sound);

        // A failure carries its message to a modal dialog (literal NSAlert parity). MessageBox.Show runs a
        // nested message loop, so the restore timer still fires while it is open — benign, and arguably
        // nicer than macOS where the modal blocks the restore. MessageBoxIcon.Warning may add its own
        // sound on top of the explicit Hand; the explicit SystemSounds policy above is authoritative.
        if (feedback.DialogMessage is { } message)
        {
            MessageBox.Show(message, "ClipRef", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void FlashIcon(SaveFeedback.IconKind kind)
    {
        if (_trayIcon is null)
        {
            return;
        }

        _trayIcon.Icon = IconFor(kind);
        _restoreTimer.Stop();
        _restoreTimer.Start();
    }

    private void RestoreDefaultIcon(object? sender, EventArgs e)
    {
        _restoreTimer.Stop();
        if (_trayIcon is not null && _defaultIcon is not null)
        {
            _trayIcon.Icon = _defaultIcon;
        }
    }

    private Icon IconFor(SaveFeedback.IconKind kind) => kind switch
    {
        SaveFeedback.IconKind.Success => _successIcon,
        SaveFeedback.IconKind.Warning => _warningIcon,
        SaveFeedback.IconKind.Failure => _failureIcon,
        _ => _warningIcon,
    };

    private static void PlaySound(SaveFeedback.SoundKind kind)
    {
        switch (kind)
        {
            case SaveFeedback.SoundKind.Success:
                SystemSounds.Asterisk.Play();
                break;
            default:
                SystemSounds.Hand.Play();
                break;
        }
    }

    public void Dispose()
    {
        // Owns only the timer and the three flash icons; the tray icon and default image belong to the host.
        _restoreTimer.Dispose();
        _successIcon.Dispose();
        _warningIcon.Dispose();
        _failureIcon.Dispose();
    }
}
