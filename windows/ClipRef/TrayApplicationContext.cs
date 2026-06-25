namespace ClipRef;

/// <summary>
/// Windowless host for the ClipRef tray icon — the Windows analogue of the macOS menu-bar agent
/// (no main window, no taskbar entry). Owns the <see cref="NotifyIcon"/> and its <see cref="Icon"/>
/// for the application's lifetime and removes the icon from the notification area on exit.
///
/// A single left-click saves the clipboard; a right-click opens the context menu (Save Clipboard Now,
/// Open Folder, Change Folder…, Launch at Login, Quit). All behavior is delegated to the WinForms-free
/// <see cref="TrayActions"/>, so this host stays thin integration glue (ADR-0009). Save-outcome
/// feedback (icon flash, sounds, error dialog) is a later item, as is the live launch-at-login
/// registry wiring — the menu entry is a disabled placeholder for now.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly TrayActions _actions;
    private readonly Icon _icon;
    private readonly NotifyIcon _trayIcon;
    private ToolStripMenuItem _folderHeader = null!;

    public TrayApplicationContext(TrayActions actions)
    {
        _actions = actions;
        _icon = LoadTrayIcon();
        _trayIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "ClipRef — left-click: save · right-click: menu",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
        _trayIcon.MouseClick += OnTrayIconClick;
    }

    private static Icon LoadTrayIcon()
    {
        using var stream = typeof(TrayApplicationContext).Assembly.GetManifestResourceStream("ClipRef.ico")
            ?? throw new InvalidOperationException("Embedded tray icon 'ClipRef.ico' is missing.");
        return new Icon(stream);
    }

    private void OnTrayIconClick(object? sender, MouseEventArgs e)
    {
        // Single left-click saves; right-click is shown by the ContextMenuStrip automatically.
        if (e.Button == MouseButtons.Left)
        {
            _actions.SaveNow();
        }
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        _folderHeader = new ToolStripMenuItem { Enabled = false };
        menu.Items.Add(_folderHeader);
        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Save Clipboard Now", null, (_, _) => _actions.SaveNow());
        menu.Items.Add("Open Folder", null, (_, _) => _actions.OpenFolder());
        menu.Items.Add("Change Folder…", null, (_, _) => _actions.ChangeFolder());
        menu.Items.Add(new ToolStripSeparator());

        // Disabled placeholder: the live launch-at-login (HKCU\…\Run) wiring is a later item.
        menu.Items.Add(new ToolStripMenuItem("Launch at Login") { Enabled = false, Checked = false });
        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Quit", null, (_, _) => Quit());

        // Refresh the folder header each time the menu opens, so it tracks a Change Folder… edit.
        menu.Opening += (_, _) => _folderHeader.Text = $"Saves → {_actions.CurrentFolder}";
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
