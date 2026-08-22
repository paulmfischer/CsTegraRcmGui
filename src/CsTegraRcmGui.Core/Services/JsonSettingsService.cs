using System.Text.Json;
using CsTegraRcmGui.Core.Models;

namespace CsTegraRcmGui.Core.Services;

/// <summary>
/// Cross-platform stand-in for the original app's registry/CArray-based
/// settings and favorites storage. Lives under the OS-standard config dir
/// (e.g. ~/.config/CsTegraRcmGui on Linux, %AppData%\CsTegraRcmGui on Windows).
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private readonly string _filePath;

    public AppSettings Current { get; private set; }

    public JsonSettingsService(string? configDirOverride = null)
    {
        var configDir = configDirOverride ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CsTegraRcmGui");

        Directory.CreateDirectory(configDir);
        _filePath = Path.Combine(configDir, "settings.json");

        Current = Load();
    }

    private AppSettings Load()
    {
        if (!File.Exists(_filePath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
