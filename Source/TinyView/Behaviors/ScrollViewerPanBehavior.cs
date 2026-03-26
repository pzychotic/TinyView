using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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

        /// <summary>
        /// When true, a viewport-sized margin is applied to the ScrollViewer content so
        /// the image can be panned until it is just outside the visible area.
        /// Bind to the ViewModel's HasImage property.
        /// </summary>
        public static readonly DependencyProperty IsOverpanEnabledProperty =
            DependencyProperty.Register(
                nameof(IsOverpanEnabled),
                typeof(bool),
                typeof(ScrollViewerPanBehavior),
                new PropertyMetadata(false, OnIsOverpanEnabledChanged));

        public bool IsOverpanEnabled
        {
            get => (bool)GetValue(IsOverpanEnabledProperty);
            set => SetValue(IsOverpanEnabledProperty, value);
        }

        private static void OnIsOverpanEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewerPanBehavior behavior && behavior.AssociatedObject != null)
            {
                behavior.UpdateMargin();
                behavior.DeferCenterContent();
            }
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
            AssociatedObject.SizeChanged += OnScrollViewerSizeChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
            AssociatedObject.PreviewMouseMove -= OnPreviewMouseMove;
            AssociatedObject.LostMouseCapture -= OnLostMouseCapture;
            AssociatedObject.SizeChanged -= OnScrollViewerSizeChanged;
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

            UpdateMargin();
            DeferCenterContent();
        }

        /// <summary>
        /// Sets the margin on the ScrollViewer's content wrapper to create the
        /// overpan area. The margin equals the viewport size on each side so the
        /// image can be scrolled just outside the visible area.
        /// </summary>
        public void UpdateMargin()
        {
            if (AssociatedObject?.Content is not FrameworkElement wrapper)
                return;

            if (!IsOverpanEnabled)
            {
                wrapper.Margin = new Thickness(0);
                return;
            }

            double vw = AssociatedObject.ViewportWidth;
            double vh = AssociatedObject.ViewportHeight;
            wrapper.Margin = new Thickness(vw, vh, vw, vh);
        }

        /// <summary>
        /// Defers centering the content until after the next layout pass so that
        /// the ScrollViewer's extent reflects any margin changes.
        /// </summary>
        private void DeferCenterContent()
        {
            if (!IsOverpanEnabled)
            {
                AssociatedObject.ScrollToHorizontalOffset(0);
                AssociatedObject.ScrollToVerticalOffset(0);
                return;
            }

            AssociatedObject.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, CenterContent);
        }

        /// <summary>
        /// Scrolls so the image is centered in the viewport.
        /// </summary>
        public void CenterContent()
        {
            AssociatedObject.ScrollToHorizontalOffset(AssociatedObject.ScrollableWidth / 2.0);
            AssociatedObject.ScrollToVerticalOffset(AssociatedObject.ScrollableHeight / 2.0);
        }

        private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsOverpanEnabled)
                return;

            if (AssociatedObject?.Content is not FrameworkElement wrapper)
                return;

            // Remember old margin so we can compensate the scroll offset.
            double oldMarginH = wrapper.Margin.Left;
            double oldMarginV = wrapper.Margin.Top;

            UpdateMargin();

            double deltaH = wrapper.Margin.Left - oldMarginH;
            double deltaV = wrapper.Margin.Top - oldMarginV;

            if (deltaH != 0 || deltaV != 0)
            {
                // Defer the offset compensation until the layout has been updated
                // with the new margin so that ScrollableWidth/Height are current.
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
}
