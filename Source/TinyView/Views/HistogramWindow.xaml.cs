using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace TinyView.Views;

/// <summary>
/// Modeless tool window showing the histogram of the current image. Closing it by the user
/// only hides it (and reports that through <see cref="HideRequested"/>), so its placement survives;
/// <see cref="CloseForShutdown"/> really closes it.
/// </summary>
public partial class HistogramWindow : Window
{
    private bool _allowClose;

    public HistogramWindow()
    {
        InitializeComponent();
    }

    /// <summary>Raised when the user closes the window (X button or Escape).</summary>
    public event EventHandler? HideRequested;

    public void CloseForShutdown()
    {
        _allowClose = true;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (_allowClose)
            return;

        e.Cancel = true;
        HideRequested?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape && !e.Handled)
        {
            HideRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }
}
