using System.Text.Json;

namespace ClipRef;

/// <summary>
/// An <see cref="ISettingsStore"/> backed by a JSON file (by default
/// <c>%APPDATA%\ClipRef\settings.json</c>). The file holds a flat string-to-string map.
/// Reads are fault-tolerant — a missing, unreadable, or unparseable file yields an empty
/// store rather than throwing — and writes create the containing directory as needed.
/// </summary>
internal sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly Dictionary<string, string> _values;

    internal JsonSettingsStore(string filePath)
    {
        _filePath = filePath;
        _values = Load(filePath);
    }

    /// <summary>The production location: <c>%APPDATA%\ClipRef\settings.json</c>.</summary>
    internal static string DefaultFilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ClipRef", "settings.json");

    public string? Get(string key) => _values.TryGetValue(key, out var value) ? value : null;

    public void Set(string key, string value)
    {
        _values[key] = value;

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(_values, WriteOptions));
    }

    private static Dictionary<string, string> Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return new Dictionary<string, string>();
        }
    }
}
