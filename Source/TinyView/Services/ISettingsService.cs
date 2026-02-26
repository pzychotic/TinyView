using TinyView.Models;

namespace TinyView.Services
{
    public interface ISettingsService
    {
        UserSettings? Load();
        void Save(UserSettings settings);
    }
}
