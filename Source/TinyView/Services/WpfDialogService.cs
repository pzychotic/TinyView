using System.Windows;

namespace TinyView.Services;

public sealed class WpfDialogService(Window owner) : IDialogService
{
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
        var about = new Views.AboutWindow { Owner = owner };
        about.ShowDialog();
    }

    public void RequestShutdown()
    {
        Application.Current.Shutdown();
    }
}
