namespace ClipRef;

/// <summary>
/// The seam through which the app reads and toggles its launch-at-login state, so the
/// <see cref="LoginItem"/> policy stays unit-testable without touching the OS. Capability-named and
/// mechanism-agnostic: the production implementation <see cref="RunKeyAutoStart"/> is the only piece
/// that knows about the Windows <c>HKCU\…\Run</c> registry key; tests use an in-memory fake. The
/// Windows analogue of the macOS <c>SMAppService</c> register/unregister/status surface. Mirrors the
/// other OS seams (<see cref="IFolderLauncher"/>, <see cref="IFileTagger"/>).
/// </summary>
internal interface IAutoStart
{
    /// <summary>Whether the app is currently registered to start at login.</summary>
    bool IsEnabled { get; }

    /// <summary>Registers the app to start at login. May throw if the write is denied.</summary>
    void Enable();

    /// <summary>Unregisters the app from starting at login. May throw if the write is denied.</summary>
    void Disable();
}
