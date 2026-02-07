using System.ComponentModel;
using System.Windows;
using TinyView.Behaviors;
using TinyView.ViewModels;

namespace TinyView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ImageViewModel _viewModel = new();

        public MainWindow()
        {
            InitializeComponent();

            // restore window geometry from app resources if available
            if (Application.Current.Resources.Contains("UserSettings") &&
                Application.Current.Resources["UserSettings"] is Models.UserSettings settings)
            {
                // apply size if present
                if (!double.IsNaN(settings.Width) && !double.IsNaN(settings.Height))
                {
                    Width = settings.Width;
                    Height = settings.Height;
                }

                // apply position if present
                if (!double.IsNaN(settings.Left) && !double.IsNaN(settings.Top))
                {
                    Left = settings.Left;
                    Top = settings.Top;
                }

                // restore selected palette if present
                if (!string.IsNullOrEmpty(settings.SelectedPaletteName))
                {
                    var palettes = Models.ColorPalettes.Palettes;
                    var match = palettes.FirstOrDefault(p => p.Name == settings.SelectedPaletteName);
                    if (!string.IsNullOrEmpty(match.Name))
                    {
                        _viewModel.SelectedPalette = match;
                    }
                }

                // if the saved state was maximized, defer applying until window is shown
                if (settings.IsMaximized)
                {
                    Loaded += (_, __) => WindowState = WindowState.Maximized;
                }
            }

            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // when closing, persist window state
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            try
            {
                var settings = new Models.UserSettings
                {
                    IsMaximized = WindowState == WindowState.Maximized,
                    Width = Width,
                    Height = Height,
                    Left = Left,
                    Top = Top,
                    SelectedPaletteName = _viewModel.SelectedPalette.Name
                };

                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TinyView");
                System.IO.Directory.CreateDirectory(dir);
                var path = System.IO.Path.Combine(dir, "UserSettings.json");
                var txt = System.Text.Json.JsonSerializer.Serialize(settings);
                System.IO.File.WriteAllText(path, txt);
            }
            catch
            {
                // ignore save errors
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(ImageViewModel.RawData))
            {
                // ensure we're on UI thread
                Dispatcher.Invoke(() =>
                {
                    // apply palette to new image
                    _viewModel.ApplyPalette();

                    // reset zoom factor to default
                    _viewModel.ZoomFactor = 1.0;

                    // reset any panning offsets on the scroll viewer
                    ScrollViewerPanBehavior.ResetPan(ImageScrollViewer);
                });
            }
        }
    }
}
