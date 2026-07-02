namespace TinyView.Services;

public interface IDialogService
{
    string? ShowOpenFileDialog(string filter);
    string? ShowSaveFileDialog(string filter, string defaultFileName);
    void ShowError(string title, string message);
    void ShowAbout();
    void RequestShutdown();
}
