namespace ClipRef;

/// <summary>
/// Windowless host for the ClipRef tray icon — the Windows analogue of the macOS
/// menu-bar agent (no main window, no taskbar entry). Owns the <see cref="NotifyIcon"/>
/// for the application's lifetime and removes it from the notification area on exit.
///
/// This is the scaffold: it shows a placeholder icon and a single Quit command. The
/// left-click save, the full menu, and the icon flash arrive with the later port items.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;

    public TrayApplicationContext()
    {
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "ClipRef",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
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
        }

        base.Dispose(disposing);
    }
}
