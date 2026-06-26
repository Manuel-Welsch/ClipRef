namespace ClipRef;

/// <summary>
/// Windowless host for the ClipRef tray icon — the Windows analogue of the macOS menu-bar agent
/// (no main window, no taskbar entry). Owns the <see cref="NotifyIcon"/> and its <see cref="Icon"/>
/// for the application's lifetime and removes the icon from the notification area on exit.
///
/// A single left-click saves the clipboard; a right-click opens the context menu (Save Clipboard Now,
/// Open Folder, Change Folder…, Launch at Login, Quit). All behavior is delegated to the WinForms-free
/// <see cref="TrayActions"/> and <see cref="LoginItem"/>, so this host stays thin integration glue
/// (ADR-0009/0011). A save flashes the tray icon, plays a sound, and shows an error dialog on failure via
/// the injected <see cref="WinFormsSaveFeedback"/> adapter (bound to this icon through
/// <see cref="WinFormsSaveFeedback.Attach"/>). Launch at Login is a live, checkable toggle whose check
/// reflects the registry state and whose failures surface in a dialog.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly TrayActions _actions;
    private readonly WinFormsSaveFeedback _feedback;
    private readonly LoginItem _loginItem;
    private readonly Icon _icon;
    private readonly NotifyIcon _trayIcon;
    private ToolStripMenuItem _folderHeader = null!;
    private ToolStripMenuItem _launchAtLoginItem = null!;

    public TrayApplicationContext(TrayActions actions, WinFormsSaveFeedback feedback, LoginItem loginItem)
    {
        _actions = actions;
        _feedback = feedback;
        _loginItem = loginItem;
        _icon = LoadTrayIcon();
        _trayIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "ClipRef — left-click: save · right-click: menu",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
        // Bind the feedback adapter to the live tray icon now that it exists, so it can flash and restore.
        _feedback.Attach(_trayIcon, _icon);
        _trayIcon.MouseClick += OnTrayIconClick;
    }

    private static Icon LoadTrayIcon() => EmbeddedIcon.Load("ClipRef.ico");

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

        _launchAtLoginItem = new ToolStripMenuItem("Launch at Login", null, (_, _) => ToggleLaunchAtLogin());
        menu.Items.Add(_launchAtLoginItem);
        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Quit", null, (_, _) => Quit());

        // Refresh the folder header and the Launch at Login checkmark each time the menu opens, so they
        // track a Change Folder… edit and the live registry state (e.g. a toggle made elsewhere).
        menu.Opening += (_, _) =>
        {
            _folderHeader.Text = $"Saves → {_actions.CurrentFolder}";
            _launchAtLoginItem.Checked = _loginItem.IsEnabled;
        };
        return menu;
    }

    private void ToggleLaunchAtLogin()
    {
        try
        {
            _loginItem.Toggle();
            _launchAtLoginItem.Checked = _loginItem.IsEnabled;
        }
        catch (Exception exception)
        {
            // Parity with the macOS toggleLaunchAtLogin presenting an error on a failed register/unregister.
            MessageBox.Show(
                $"Could not change the launch-at-login setting:\n{exception.Message}",
                "ClipRef",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
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
            // Adapter first: it stops its restore timer, so no pending tick touches a disposed tray icon.
            _feedback.Dispose();
            _trayIcon.Dispose();
            _icon.Dispose();
        }

        base.Dispose(disposing);
    }
}
