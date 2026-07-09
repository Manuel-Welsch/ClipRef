namespace ClipRef;

/// <summary>
/// Loads an embedded <c>.ico</c> by its manifest logical name. Shared by the tray host (the default
/// icon) and the feedback adapter (the three flash icons), so the manifest-stream boilerplate lives in
/// one place.
/// </summary>
internal static class EmbeddedIcon
{
    internal static Icon Load(string logicalName)
    {
        using var stream = typeof(EmbeddedIcon).Assembly.GetManifestResourceStream(logicalName)
            ?? throw new InvalidOperationException($"Embedded icon '{logicalName}' is missing.");
        return new Icon(stream);
    }
}
