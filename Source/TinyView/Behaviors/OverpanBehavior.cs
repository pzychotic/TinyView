using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TinyView.Models;

namespace TinyView.Behaviors;

/// <summary>
/// Adds a viewport-sized margin around the ScrollViewer content so the image can
/// be panned until it is just outside the visible area.  Compensates scroll offsets
/// when the viewport is resized and re-centers content on reset.
/// </summary>
public sealed class OverpanBehavior : Behavior<ScrollViewer>
{
    /// <summary>
    /// When true the overpan margin is applied; when false it is removed.
    /// Bind to the ViewModel's HasImage property.
    /// </summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(
            nameof(IsEnabled),
            typeof(bool),
            typeof(OverpanBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public bool IsEnabled
    {
        get => (bool)GetValue(IsEnabledProperty);
        set => SetValue(IsEnabledProperty, value);
    }

    /// <summary>
    /// Optional notifier that signals a viewport reset (e.g. when a new image is loaded).
    /// When fired the margin is recalculated and the content is centered.
    /// </summary>
    public static readonly DependencyProperty ResetNotifierProperty =
        DependencyProperty.Register(
            nameof(ResetNotifier),
            typeof(ViewportResetNotifier),
            typeof(OverpanBehavior),
            new PropertyMetadata(null, OnResetNotifierChanged));

    public ViewportResetNotifier? ResetNotifier
    {
        get => (ViewportResetNotifier?)GetValue(ResetNotifierProperty);
        set => SetValue(ResetNotifierProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OverpanBehavior behavior && behavior.AssociatedObject != null)
            behavior.ResetToCenter();
    }

    private static void OnResetNotifierChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not OverpanBehavior behavior)
            return;

        if (e.OldValue is ViewportResetNotifier old)
            old.ResetRequested -= behavior.OnResetRequested;

        if (e.NewValue is ViewportResetNotifier notifier)
            notifier.ResetRequested += behavior.OnResetRequested;
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.SizeChanged += OnScrollViewerSizeChanged;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.SizeChanged -= OnScrollViewerSizeChanged;

        // unsubscribe from notifier
        if (ResetNotifier is { } notifier)
            notifier.ResetRequested -= OnResetRequested;

        base.OnDetaching();
    }

    private void OnResetRequested() => ResetToCenter();

    /// <summary>
    /// Updates the margin and defers centering so the ScrollViewer's extent
    /// reflects any margin changes before offsets are set.
    /// </summary>
    internal void ResetToCenter()
    {
        UpdateMargin();
        DeferCenterContent();
    }

    /// <summary>
    /// Sets the margin on the ScrollViewer's content wrapper to create the
    /// overpan area.  The margin equals the viewport size on each side.
    /// </summary>
    internal void UpdateMargin()
    {
        if (AssociatedObject?.Content is not FrameworkElement wrapper)
            return;

        if (!IsEnabled)
        {
            wrapper.Margin = new Thickness(0);
            return;
        }

        double vw = AssociatedObject.ViewportWidth;
        double vh = AssociatedObject.ViewportHeight;
        wrapper.Margin = new Thickness(vw, vh, vw, vh);
    }

    private void DeferCenterContent()
    {
        if (!IsEnabled)
        {
            AssociatedObject.ScrollToHorizontalOffset(0);
            AssociatedObject.ScrollToVerticalOffset(0);
            return;
        }

        AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, CenterContent);
    }

    internal void CenterContent()
    {
        AssociatedObject.ScrollToHorizontalOffset(AssociatedObject.ScrollableWidth / 2.0);
        AssociatedObject.ScrollToVerticalOffset(AssociatedObject.ScrollableHeight / 2.0);
    }

    private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!IsEnabled)
            return;

        if (AssociatedObject?.Content is not FrameworkElement wrapper)
            return;

        double oldMarginH = wrapper.Margin.Left;
        double oldMarginV = wrapper.Margin.Top;

        UpdateMargin();

        double deltaH = wrapper.Margin.Left - oldMarginH;
        double deltaV = wrapper.Margin.Top - oldMarginV;

        if (deltaH != 0 || deltaV != 0)
        {
            double targetH = AssociatedObject.HorizontalOffset + deltaH;
            double targetV = AssociatedObject.VerticalOffset + deltaV;
            AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                AssociatedObject.ScrollToHorizontalOffset(targetH);
                AssociatedObject.ScrollToVerticalOffset(targetV);
            });
        }
    }
}
