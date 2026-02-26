using System.Globalization;
using System.Windows;
using TinyView.Services;

namespace TinyView
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var invariant = CultureInfo.InvariantCulture;

            // Ensure the current thread uses invariant culture
            Thread.CurrentThread.CurrentCulture = invariant;
            Thread.CurrentThread.CurrentUICulture = invariant;

            // Ensure future threads default to invariant culture
            CultureInfo.DefaultThreadCurrentCulture = invariant;
            CultureInfo.DefaultThreadCurrentUICulture = invariant;

            // attempt to restore last window state (if available)
            var settingsService = new JsonSettingsService();
            var settings = settingsService.Load();
            if (settings != null)
                Resources["UserSettings"] = settings;

            // make settings service available to the window
            Resources["SettingsService"] = settingsService;

            base.OnStartup(e);
        }
    }
}
