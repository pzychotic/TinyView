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
    private readonly ImageViewModel _viewModel = new(new WpfDialogService());
    private readonly ISettingsService _settingsService;

    public MainWindow(ISettingsService settingsService, UserSettings? settings)
    {
        _settingsService = settingsService;

        InitializeComponent();

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
                // ensure coordinates are within the virtual screen bounds to prevent off-screen windows
                double virtualLeft = SystemParameters.VirtualScreenLeft;
                double virtualTop = SystemParameters.VirtualScreenTop;
                double virtualWidth = SystemParameters.VirtualScreenWidth;
                double virtualHeight = SystemParameters.VirtualScreenHeight;

                bool isWithinBounds = settings.Left >= virtualLeft &&
                                      settings.Top >= virtualTop &&
                                      settings.Left < (virtualLeft + virtualWidth) &&
                                      settings.Top < (virtualTop + virtualHeight);

                if (isWithinBounds)
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
        var settings = new UserSettings
        {
            IsMaximized = WindowState == WindowState.Maximized,
            Width = Width,
            Height = Height,
            Left = Left,
            Top = Top,
            SelectedPaletteName = _viewModel.SelectedPaletteName
        };

        _settingsService.Save(settings);
    }
}
