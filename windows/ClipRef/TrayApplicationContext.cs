namespace ClipRef;

/// <summary>
/// Windowless host for the ClipRef tray icon — the Windows analogue of the macOS menu-bar agent
/// (no main window, no taskbar entry). Owns the <see cref="NotifyIcon"/> and its <see cref="Icon"/>
/// for the application's lifetime and removes the icon from the notification area on exit.
///
/// This item gives the shell its real clipboard-glyph icon and a Quit command. The left-click save,
/// the full right-click menu, and the icon flash/sound arrive with the later Phase 3 items.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly Icon _icon;
    private readonly NotifyIcon _trayIcon;

    public TrayApplicationContext()
    {
        _icon = LoadTrayIcon();
        _trayIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "ClipRef",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
    }

    private static Icon LoadTrayIcon()
    {
        using var stream = typeof(TrayApplicationContext).Assembly.GetManifestResourceStream("ClipRef.ico")
            ?? throw new InvalidOperationException("Embedded tray icon 'ClipRef.ico' is missing.");
        return new Icon(stream);
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Quit", null, (_, _) => Quit());
        return menu;
    }

    private void Quit()
    {
        _trayIcon.Visible = false;
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _icon.Dispose();
        }

        base.Dispose(disposing);
    }
}
