using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TinyView.Models;

namespace TinyView.Behaviors;

/// <summary>
/// Enables click-and-drag panning on a <see cref="ScrollViewer"/>.
/// This behavior is responsible only for mouse-driven panning; zoom
/// compensation and overpan margin management are handled by
/// <see cref="ZoomCompensationBehavior"/> and <see cref="OverpanBehavior"/>.
/// </summary>
public sealed class ScrollViewerPanBehavior : Behavior<ScrollViewer>
{
    /// <summary>
    /// Optional notifier that signals a viewport reset (e.g. when a new image is loaded).
    /// When fired the active pan operation is cancelled.
    /// </summary>
    public static readonly DependencyProperty ResetNotifierProperty =
        DependencyProperty.Register(
            nameof(ResetNotifier),
            typeof(ViewportResetNotifier),
            typeof(ScrollViewerPanBehavior),
            new PropertyMetadata(null, OnResetNotifierChanged));

    public ViewportResetNotifier? ResetNotifier
    {
        get => (ViewportResetNotifier?)GetValue(ResetNotifierProperty);
        set => SetValue(ResetNotifierProperty, value);
    }

    private static void OnResetNotifierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewerPanBehavior behavior)
            return;

        if (e.OldValue is ViewportResetNotifier old)
            old.ResetRequested -= behavior.OnResetRequested;

        if (e.NewValue is ViewportResetNotifier notifier)
            notifier.ResetRequested += behavior.OnResetRequested;
    }

    internal bool _isPanning;
    internal Point _panStartPoint;
    internal Point _panStartOffset;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown;
        AssociatedObject.PreviewMouseRightButtonUp += OnPreviewMouseRightButtonUp;
        AssociatedObject.PreviewMouseMove += OnPreviewMouseMove;
        AssociatedObject.LostMouseCapture += OnLostMouseCapture;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseRightButtonDown -= OnPreviewMouseRightButtonDown;
        AssociatedObject.PreviewMouseRightButtonUp -= OnPreviewMouseRightButtonUp;
        AssociatedObject.PreviewMouseMove -= OnPreviewMouseMove;
        AssociatedObject.LostMouseCapture -= OnLostMouseCapture;

        // unsubscribe from notifier
        if (ResetNotifier is { } notifier)
            notifier.ResetRequested -= OnResetRequested;

        base.OnDetaching();
    }

    private void OnResetRequested() => CancelPan();

    private void OnPreviewMouseRightButtonDown(object? sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject dep && IsOverScrollbar(dep))
            return;

        if (!AssociatedObject.IsMouseOver)
            return;

        _isPanning = true;
        _panStartPoint = e.GetPosition(AssociatedObject);
        _panStartOffset = new Point(AssociatedObject.HorizontalOffset, AssociatedObject.VerticalOffset);
        AssociatedObject.CaptureMouse();
        AssociatedObject.Cursor = Cursors.Hand;

        e.Handled = true;
    }

    private void OnPreviewMouseRightButtonUp(object? sender, MouseButtonEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            AssociatedObject.ReleaseMouseCapture();
            AssociatedObject.Cursor = null;
            e.Handled = true;
        }
    }

    private void OnLostMouseCapture(object? sender, MouseEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            AssociatedObject.Cursor = null;
        }
    }

    private void OnPreviewMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isPanning)
            return;

        var currentPoint = e.GetPosition(AssociatedObject);
        var delta = currentPoint - _panStartPoint;

        double newH = _panStartOffset.X - delta.X;
        double newV = _panStartOffset.Y - delta.Y;

        AssociatedObject.ScrollToHorizontalOffset(newH);
        AssociatedObject.ScrollToVerticalOffset(newV);

        e.Handled = true;
    }

    internal static bool IsOverScrollbar(DependencyObject? dep)
    {
        while (dep != null)
        {
            if (dep is ScrollBar || dep is Thumb || dep is RepeatButton)
                return true;

            dep = VisualTreeHelper.GetParent(dep);
        }
        return false;
    }

    /// <summary>
    /// Cancel any active pan operation, release capture, and restore the cursor.
    /// </summary>
    internal void CancelPan()
    {
        if (_isPanning)
        {
            _isPanning = false;
            AssociatedObject.ReleaseMouseCapture();
            AssociatedObject.Cursor = null;
        }

        _panStartPoint = default;
        _panStartOffset = default;
    }
}
