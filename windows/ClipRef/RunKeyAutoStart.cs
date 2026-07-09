using Microsoft.Win32;

namespace ClipRef;

/// <summary>
/// The production <see cref="IAutoStart"/> adapter over the per-user
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c> registry key — the Windows analogue of the
/// macOS <c>SMAppService</c> registration. Presence of the <c>ClipRef</c> value (the quoted tray exe
/// path, no args) means enabled. It holds no policy — the launch-at-login rules live in
/// <see cref="LoginItem"/> — so, like <see cref="ExplorerFolderLauncher"/> and <see cref="NtfsFileTagger"/>,
/// it is integration-only and not unit-tested (ADR-0011).
/// </summary>
internal sealed class RunKeyAutoStart : IAutoStart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ClipRef";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(ValueName) != null;
        }
    }

    public void Enable()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(ValueName, Command);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    // The running tray exe, quoted so a profile path with spaces still parses as one argument. On a
    // WinExe Environment.ProcessPath is the apphost (ClipRef.exe), not the dotnet host — exactly what
    // should auto-start; it is non-null for a launched process.
    private static string Command => "\"" + Environment.ProcessPath + "\"";
}
