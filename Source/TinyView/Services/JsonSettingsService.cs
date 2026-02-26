using System.IO;
using System.Text.Json;
using TinyView.Models;

namespace TinyView.Services
{
    public sealed class JsonSettingsService : ISettingsService
    {
        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TinyView");

        private static readonly string SettingsPath =
            Path.Combine(SettingsDir, "UserSettings.json");

        public UserSettings? Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var txt = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<UserSettings>(txt);
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
                Directory.CreateDirectory(SettingsDir);
                var txt = JsonSerializer.Serialize(settings);
                File.WriteAllText(SettingsPath, txt);
            }
            catch
            {
                // ignore save errors
            }
        }
    }
}
