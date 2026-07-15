using System.IO;
using System.Text.Json;
using TinyView.Models;

namespace TinyView.Services;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON in the user's roaming profile.
/// Persistence is best-effort: any I/O or parse failure falls back to defaults silently.
/// </summary>
public sealed class JsonSettingsService(string settingsDir) : ISettingsService
{
    private static readonly string DefaultSettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TinyView");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _settingsDir = settingsDir;
    private readonly string _settingsPath = Path.Combine(settingsDir, "Settings.json");

    public JsonSettingsService()
        : this(DefaultSettingsDir)
    {
    }

    public AppSettings? Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var txt = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(txt, JsonOptions);
            }
        }
        catch
        {
            // ignore errors restoring settings
        }

        return null;
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(_settingsDir);
            var txt = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsPath, txt);
        }
        catch
        {
            // Losing the saved state must never break the application.
        }
    }
}
