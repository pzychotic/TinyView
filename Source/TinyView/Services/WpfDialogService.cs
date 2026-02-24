using System.Windows;

namespace TinyView.Services
{
    public class WpfDialogService : IDialogService
    {
        public string? ShowOpenFileDialog(string filter)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog { Filter = filter };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public void ShowError(string title, string message)
        {
            MessageBox.Show(Application.Current.MainWindow, message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowAbout()
        {
            var about = new Views.AboutWindow { Owner = Application.Current.MainWindow };
            about.ShowDialog();
        }
    }
}
