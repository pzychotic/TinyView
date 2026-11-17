namespace TinyView.Services;

public interface IDialogService
{
    string? ShowOpenFileDialog(string filter);
    string? ShowSaveFileDialog(string filter, string defaultFileName);
    void ShowError(string title, string message);
    void ShowAbout();

    /// <summary>
    /// Shows the modeless histogram window bound to <paramref name="dataContext"/>.
    /// <paramref name="closeRequested"/> is invoked when the user closes the window.
    /// </summary>
    void ShowHistogram(object dataContext, Action closeRequested);

    /// <summary>Hides the histogram window if it is shown.</summary>
    void HideHistogram();
    void RequestShutdown();
}
