using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace TinyView.Behaviors
{
    public class ScrollViewerPanBehavior : Behavior<ScrollViewer>
    {
        /// <summary>
        /// Bindable trigger property. Each time the value changes, the pan state is reset
        /// and the ScrollViewer scrolls back to the origin.
        /// </summary>
        public static readonly DependencyProperty ResetTriggerProperty =
            DependencyProperty.Register(
                nameof(ResetTrigger),
                typeof(bool),
                typeof(ScrollViewerPanBehavior),
                new PropertyMetadata(false, OnResetTriggerChanged));

        public bool ResetTrigger
        {
            get => (bool)GetValue(ResetTriggerProperty);
            set => SetValue(ResetTriggerProperty, value);
        }

        private static void OnResetTriggerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewerPanBehavior behavior && behavior.AssociatedObject != null)
                behavior.ResetPan();
        }

        private bool _isPanning;
        private Point _panStartPoint;
        private Point _panStartOffset;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            AssociatedObject.PreviewMouseMove += OnPreviewMouseMove;
            AssociatedObject.LostMouseCapture += OnLostMouseCapture;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
            AssociatedObject.PreviewMouseMove -= OnPreviewMouseMove;
            AssociatedObject.LostMouseCapture -= OnLostMouseCapture;
            base.OnDetaching();
        }

        private void OnPreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            // prevent scrollbar and its parts to handle the event
            if (e.OriginalSource is DependencyObject dep && IsOverScrollbar(dep))
                return;

            if (!AssociatedObject.IsMouseOver)
                return;

            _isPanning = true;
            _panStartPoint = e.GetPosition(AssociatedObject);
            _panStartOffset = new Point(AssociatedObject.HorizontalOffset, AssociatedObject.VerticalOffset);
            // capture mouse so we continue to receive events while dragging
            AssociatedObject.CaptureMouse();
            AssociatedObject.Cursor = Cursors.Hand;

            e.Handled = true;
        }

        private void OnPreviewMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
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
            // ensure state is cleared and cursor restored if capture is lost unexpectedly
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

            // invert delta so dragging the mouse moves the image in the expected direction
            double newH = _panStartOffset.X - delta.X;
            double newV = _panStartOffset.Y - delta.Y;

            // clamp to scrollable extents
            newH = Math.Clamp(newH, 0, AssociatedObject.ScrollableWidth);
            newV = Math.Clamp(newV, 0, AssociatedObject.ScrollableHeight);

            AssociatedObject.ScrollToHorizontalOffset(newH);
            AssociatedObject.ScrollToVerticalOffset(newV);

            e.Handled = true;
        }

        private static bool IsOverScrollbar(DependencyObject? dep)
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
        /// Reset any panning state on the provided ScrollViewer and scroll to the origin (0,0).
        /// </summary>
        public void ResetPan()
        {
            // if a panning operation is active ensure we clear capture and state
            if (_isPanning)
            {
                _isPanning = false;
                AssociatedObject.ReleaseMouseCapture();
                AssociatedObject.Cursor = null;
            }

            // reset start point/offset to defaults
            _panStartPoint = default;
            _panStartOffset = default;

            // scroll to top-left
            AssociatedObject.ScrollToHorizontalOffset(0);
            AssociatedObject.ScrollToVerticalOffset(0);
        }
    }
}
