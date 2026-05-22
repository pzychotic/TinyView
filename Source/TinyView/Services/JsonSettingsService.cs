using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TinyView.Models;

namespace TinyView.Services;

public sealed class JsonSettingsService(string settingsDir) : ISettingsService
{
    private static readonly string DefaultSettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TinyView");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    private readonly string _settingsDir = settingsDir;
    private readonly string _settingsPath = Path.Combine(settingsDir, "UserSettings.json");

    public JsonSettingsService()
        : this(DefaultSettingsDir)
    {
    }

    public UserSettings? Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var txt = File.ReadAllText(_settingsPath);
                return JsonSerializer.Deserialize<UserSettings>(txt, JsonOptions);
            }
        }
        catch
        {
            // ignore errors restoring settings
        }

        return null;
    }

    public void Save(UserSettings settings)
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
