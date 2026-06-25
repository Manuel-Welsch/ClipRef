using ClipRef;

namespace ClipRefTests;

/// <summary>
/// In-memory <see cref="IFolderPicker"/> test double — returns a preset path to model the user
/// picking a folder, or <c>null</c> to model Cancel, and records the initial path it was seeded with
/// so the change-folder action can be asserted without a modal dialog.
/// </summary>
internal sealed class FakeFolderPicker : IFolderPicker
{
    private readonly string? _result;

    internal FakeFolderPicker(string? result)
    {
        _result = result;
    }

    internal bool WasShown { get; private set; }

    internal string? SeededWith { get; private set; }

    public string? PickFolder(string initialPath)
    {
        WasShown = true;
        SeededWith = initialPath;
        return _result;
    }
}
