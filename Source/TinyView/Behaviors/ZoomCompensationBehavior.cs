using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace TinyView.Behaviors
{
    /// <summary>
    /// Adjusts ScrollViewer offsets after a zoom factor change so that the anchor
    /// point (cursor position or viewport center) remains at the same screen location.
    /// </summary>
    public class ZoomCompensationBehavior : Behavior<ScrollViewer>
    {
        /// <summary>
        /// Current zoom factor, bound to the ViewModel's Zoom.Factor.
        /// When the value changes, scroll offsets are adjusted to keep the anchor stable.
        /// </summary>
        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register(
                nameof(ZoomFactor),
                typeof(double),
                typeof(ZoomCompensationBehavior),
                new PropertyMetadata(1.0, OnZoomFactorChanged));

        public double ZoomFactor
        {
            get => (double)GetValue(ZoomFactorProperty);
            set => SetValue(ZoomFactorProperty, value);
        }

        /// <summary>
        /// Viewport-relative anchor point for the current zoom operation.
        /// Bound two-way to Zoom.Anchor.  Read and cleared each time
        /// <see cref="ZoomFactor"/> changes; when null the viewport center is used.
        /// </summary>
        public static readonly DependencyProperty ZoomAnchorProperty =
            DependencyProperty.Register(
                nameof(ZoomAnchor),
                typeof(Point?),
                typeof(ZoomCompensationBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Point? ZoomAnchor
        {
            get => (Point?)GetValue(ZoomAnchorProperty);
            set => SetValue(ZoomAnchorProperty, value);
        }

        private static void OnZoomFactorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ZoomCompensationBehavior behavior && behavior.AssociatedObject != null)
            {
                double oldZoom = (double)e.OldValue;
                double newZoom = (double)e.NewValue;
                if (oldZoom > 0 && newZoom > 0 && oldZoom != newZoom)
                    behavior.AdjustScrollForZoom(oldZoom, newZoom);
            }
        }

        private void AdjustScrollForZoom(double oldZoom, double newZoom)
        {
            var sv = AssociatedObject;

            // Read and clear the one-shot anchor.
            Point? explicitAnchor = ZoomAnchor;
            ZoomAnchor = null;

            // Anchor in viewport coordinates (fallback to viewport center).
            Point anchor = explicitAnchor ?? new Point(sv.ViewportWidth / 2.0, sv.ViewportHeight / 2.0);

            double ratio = newZoom / oldZoom;

            // The content-space point under the anchor, accounting for overpan margin.
            var wrapper = sv.Content as FrameworkElement;
            double marginH = wrapper?.Margin.Left ?? 0;
            double marginV = wrapper?.Margin.Top ?? 0;

            double cpH = sv.HorizontalOffset + anchor.X;
            double cpV = sv.VerticalOffset + anchor.Y;

            // The margin portion of the content is constant; only the image portion scales.
            double newCpH = marginH + (cpH - marginH) * ratio;
            double newCpV = marginV + (cpV - marginV) * ratio;

            double targetH = newCpH - anchor.X;
            double targetV = newCpV - anchor.Y;

            sv.ScrollToHorizontalOffset(targetH);
            sv.ScrollToVerticalOffset(targetV);
        }
    }
}
