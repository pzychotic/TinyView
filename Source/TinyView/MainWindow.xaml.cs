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
    private readonly AppSettings _settings;
    private readonly WpfDialogService _dialogService;

    public MainWindow(ISettingsService settingsService, AppSettings settings)
    {
        _settingsService = settingsService;
        _settings = settings;

        InitializeComponent();

        _dialogService = new WpfDialogService(this);
        _viewModel = new(_dialogService);
        _viewModel.RestorePalette(settings.SelectedPaletteName);
        DataContext = _viewModel;

        RestorePlacement(settings);
        RestoreHistogram(settings);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (!e.Cancel)
        {
            SavePlacement(_settings);
            SaveHistogram(_settings);
            _settings.SelectedPaletteName = _viewModel.SelectedPaletteName;
            _dialogService.CloseHistogram();
            _settingsService.Save(_settings);
        }
    }

    private void RestorePlacement(AppSettings settings)
    {
        if (settings.WindowLeft is not double left || settings.WindowTop is not double top ||
            settings.WindowWidth is not double width || settings.WindowHeight is not double height)
            return;

        if (!IsOnScreen(new Rect(left, top, width, height)))
            return;

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = left;
        Top = top;
        Width = width;
        Height = height;

        if (settings.WindowMaximized)
            WindowState = WindowState.Maximized;
    }

    private void SavePlacement(AppSettings settings)
    {
        // When maximized or minimized, RestoreBounds holds the normal-state geometry.
        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;

        settings.WindowLeft = bounds.Left;
        settings.WindowTop = bounds.Top;
        settings.WindowWidth = bounds.Width;
        settings.WindowHeight = bounds.Height;
        settings.WindowMaximized = WindowState == WindowState.Maximized;
    }

    private void RestoreHistogram(AppSettings settings)
    {
        if (settings.HistogramLogScale is bool logScale)
            _viewModel.IsHistogramLogScale = logScale;

        if (settings.HistogramLeft is double left && settings.HistogramTop is double top &&
            settings.HistogramWidth is double width && settings.HistogramHeight is double height)
        {
            var bounds = new Rect(left, top, width, height);
            if (IsOnScreen(bounds))
                _dialogService.HistogramBounds = bounds;
        }

        // An owned window can only be shown after its owner has been shown.
        if (settings.HistogramVisible)
            ContentRendered += (_, _) => _viewModel.IsHistogramVisible = true;
    }

    private void SaveHistogram(AppSettings settings)
    {
        settings.HistogramVisible = _viewModel.IsHistogramVisible;
        settings.HistogramLogScale = _viewModel.IsHistogramLogScale;

        if (_dialogService.HistogramBounds is Rect bounds)
        {
            settings.HistogramLeft = bounds.Left;
            settings.HistogramTop = bounds.Top;
            settings.HistogramWidth = bounds.Width;
            settings.HistogramHeight = bounds.Height;
        }
    }

    // Ignore stale bounds that no longer touch any monitor (e.g. a display was unplugged).
    private static bool IsOnScreen(Rect bounds)
    {
        var virtualScreen = new Rect(
            SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        return virtualScreen.IntersectsWith(bounds);
    }
}
