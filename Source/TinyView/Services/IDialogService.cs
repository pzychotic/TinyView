namespace TinyView.Services
{
    public interface IDialogService
    {
        string? ShowOpenFileDialog(string filter);
        void ShowError(string title, string message);
        void ShowAbout();
    }
}
