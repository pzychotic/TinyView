using TinyView.Models;

namespace TinyView.Services;

public interface ISettingsService
{
    AppSettings? Load();
    void Save(AppSettings settings);
}
