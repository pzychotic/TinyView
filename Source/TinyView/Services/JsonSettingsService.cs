using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TinyView.Models;

namespace TinyView.Services;

public sealed class JsonSettingsService(string settingsDir) : ISettingsService
{
    private static readonly string DefaultSettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TinyView");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        WriteIndented = true
    };

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
            // ignore save errors
        }
    }
}
