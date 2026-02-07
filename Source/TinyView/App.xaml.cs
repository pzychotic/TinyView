using System.Globalization;
using System.Windows;

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
            try
            {
                var path = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "TinyView", "UserSettings.json");

                if (System.IO.File.Exists(path))
                {
                    var txt = System.IO.File.ReadAllText(path);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<Models.UserSettings>(txt);
                    // store onto App resources so MainWindow can pick it up during construction
                    if (settings != null)
                        Resources["UserSettings"] = settings;
                }
            }
            catch
            {
                // ignore errors restoring settings
            }

            base.OnStartup(e);
        }
    }
}
