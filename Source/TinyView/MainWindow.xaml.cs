using System.ComponentModel;
using System.Windows;
using TinyView.Models;
using TinyView.Services;
using TinyView.ViewModels;

namespace TinyView;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ImageViewModel _viewModel;
    private readonly ISettingsService _settingsService;

    public MainWindow(ISettingsService settingsService, AppSettings? settings)
    {
        _settingsService = settingsService;

        InitializeComponent();

        _viewModel = new(new WpfDialogService(this));

        // restore window geometry and view state from the injected settings (if any)
        if (settings != null)
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
                // Ignore stale bounds that no longer touch any monitor (e.g. a display was unplugged).
                var virtualScreen = new Rect(
                    SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                    SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);

                if (virtualScreen.IntersectsWith(new Rect(settings.Left, settings.Top, Width, Height)))
                {
                    Left = settings.Left;
                    Top = settings.Top;
                }
            }

            // restore selected palette if present
            _viewModel.RestorePalette(settings.SelectedPaletteName);

            // if the saved state was maximized, defer applying until window is shown
            if (settings.IsMaximized)
            {
                Loaded += (_, __) => WindowState = WindowState.Maximized;
            }
        }

        DataContext = _viewModel;

        // when closing, persist window state
        Closing += MainWindow_Closing;
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        // When maximized or minimized, RestoreBounds holds the normal-state geometry.
        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;

        var settings = new AppSettings
        {
            IsMaximized = WindowState == WindowState.Maximized,
            Width = bounds.Width,
            Height = bounds.Height,
            Left = bounds.Left,
            Top = bounds.Top,
            SelectedPaletteName = _viewModel.SelectedPaletteName
        };

        _settingsService.Save(settings);
    }
}
