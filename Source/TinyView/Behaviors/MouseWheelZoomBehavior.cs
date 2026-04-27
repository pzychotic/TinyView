using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace TinyView.Behaviors;

/// <summary>
/// Translates Ctrl+MouseWheel input into zoom factor changes.
/// Decoupled from scroll compensation — sets <see cref="ZoomAnchor"/>
/// (bound to <c>Zoom.Anchor</c>) before changing <see cref="ZoomFactor"/>
/// so that <see cref="ZoomCompensationBehavior"/> can keep the cursor
/// position stable.
/// </summary>
public class MouseWheelZoomBehavior : Behavior<UIElement>
{
    private const int WheelDeltaPerNotch = 120; // Win32 WHEEL_DELTA
    private const double DefaultZoomStep = 1.1; // 10% per notch

    public static readonly DependencyProperty ZoomFactorProperty =
        DependencyProperty.Register(
            nameof(ZoomFactor),
            typeof(double),
            typeof(MouseWheelZoomBehavior),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public double ZoomFactor
    {
        get => (double)GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    /// <summary>
    /// Viewport-relative anchor point for cursor-centered zoom.
    /// Bound two-way to <c>Zoom.Anchor</c> so that
    /// <see cref="ZoomCompensationBehavior"/> can consume it.
    /// </summary>
    public static readonly DependencyProperty ZoomAnchorProperty =
        DependencyProperty.Register(
            nameof(ZoomAnchor),
            typeof(Point?),
            typeof(MouseWheelZoomBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public Point? ZoomAnchor
    {
        get => (Point?)GetValue(ZoomAnchorProperty);
        set => SetValue(ZoomAnchorProperty, value);
    }

    internal double _wheelDeltaAccum;

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
        base.OnDetaching();
    }

    private void OnPreviewMouseWheel(object? sender, MouseWheelEventArgs e)
    {
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            return;

        _wheelDeltaAccum += e.Delta;

        int wholeNotches = (int)(_wheelDeltaAccum / WheelDeltaPerNotch);
        if (wholeNotches != 0)
        {
            // Set the anchor before changing the factor so the compensation
            // behavior can read it in the same dispatcher frame.
            ZoomAnchor = e.GetPosition(AssociatedObject);

            double current = ZoomFactor;
            current *= Math.Pow(DefaultZoomStep, wholeNotches);
            ZoomFactor = current;

            _wheelDeltaAccum -= wholeNotches * WheelDeltaPerNotch;
        }

        e.Handled = true;
    }
}
