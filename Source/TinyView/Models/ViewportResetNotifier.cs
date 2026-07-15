namespace TinyView.Models;

/// <summary>
/// Lightweight event source that view-layer components (behaviors) can subscribe to
/// in order to receive viewport-reset requests.
/// The ViewModel calls <see cref="RequestReset"/> (e.g. when a new image is loaded),
/// as can view components (e.g. the right-click pan reset), and any attached behavior
/// reacts by resetting its own state (cancel panning, re-center content, etc.).
/// </summary>
public sealed class ViewportResetNotifier
{
    /// <summary>
    /// Raised each time the viewport should be reset.
    /// </summary>
    public event Action? ResetRequested;

    /// <summary>
    /// Request that all subscribers reset their viewport state.
    /// </summary>
    public void RequestReset() => ResetRequested?.Invoke();
}
