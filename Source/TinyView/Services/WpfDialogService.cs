using System.Windows;
using TinyView.Views;

namespace TinyView.Services;

public sealed class WpfDialogService(Window owner) : IDialogService
{
    private HistogramWindow? _histogramWindow;
    private Action? _histogramCloseRequested;
    private Rect? _histogramBounds;

    public string? ShowOpenFileDialog(string filter)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Open Image", Filter = filter };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public string? ShowSaveFileDialog(string filter, string defaultFileName)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Title = "Export Image", Filter = filter, FileName = defaultFileName };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public void ShowError(string title, string message)
    {
        MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowAbout()
    {
        var about = new AboutWindow { Owner = owner };
        about.ShowDialog();
    }

    public void ShowHistogram(object dataContext, Action closeRequested)
    {
        _histogramCloseRequested = closeRequested;

        if (_histogramWindow == null)
        {
            _histogramWindow = new HistogramWindow { Owner = owner };
            _histogramWindow.HideRequested += (_, _) => _histogramCloseRequested?.Invoke();

            if (_histogramBounds is Rect bounds)
            {
                _histogramWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                _histogramWindow.Left = bounds.Left;
                _histogramWindow.Top = bounds.Top;
                _histogramWindow.Width = bounds.Width;
                _histogramWindow.Height = bounds.Height;
            }
        }

        _histogramWindow.DataContext = dataContext;
        _histogramWindow.Show();
    }

    public void HideHistogram()
    {
        _histogramWindow?.Hide();
        owner.Activate();
    }

    /// <summary>
    /// Placement of the histogram window: the live window's bounds once it has been created,
    /// otherwise the bounds to apply when it is first shown (e.g. restored from settings).
    /// </summary>
    public Rect? HistogramBounds
    {
        get => _histogramWindow != null
            ? new Rect(_histogramWindow.Left, _histogramWindow.Top, _histogramWindow.Width, _histogramWindow.Height)
            : _histogramBounds;
        set => _histogramBounds = value;
    }

    /// <summary>Closes the histogram window for good (application shutdown).</summary>
    public void CloseHistogram()
    {
        if (_histogramWindow == null)
            return;

        _histogramBounds = HistogramBounds;
        _histogramWindow.CloseForShutdown();
        _histogramWindow = null;
    }

    public void RequestShutdown()
    {
        Application.Current.Shutdown();
    }
}
