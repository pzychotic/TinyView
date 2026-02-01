using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace TinyView.Behaviors
{
    public static class ScrollViewerPanBehavior
    {
        public static readonly DependencyProperty EnablePanProperty =
            DependencyProperty.RegisterAttached(
                "EnablePan",
                typeof(bool),
                typeof(ScrollViewerPanBehavior),
                new PropertyMetadata(false, OnEnablePanChanged));

        public static void SetEnablePan(DependencyObject element, bool value) =>
            element.SetValue(EnablePanProperty, value);

        public static bool GetEnablePan(DependencyObject element) =>
            (bool)element.GetValue(EnablePanProperty);

        private static void OnEnablePanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer sv)
                return;

            if ((bool)e.NewValue)
                Attach(sv);
            else
                Detach(sv);
        }

        private static readonly DependencyProperty IsPanningProperty =
            DependencyProperty.RegisterAttached("IsPanning", typeof(bool), typeof(ScrollViewerPanBehavior), new PropertyMetadata(false));

        private static readonly DependencyProperty PanStartPointProperty =
            DependencyProperty.RegisterAttached("PanStartPoint", typeof(Point), typeof(ScrollViewerPanBehavior), new PropertyMetadata(default(Point)));

        private static readonly DependencyProperty PanStartOffsetProperty =
            DependencyProperty.RegisterAttached("PanStartOffset", typeof(Point), typeof(ScrollViewerPanBehavior), new PropertyMetadata(default(Point)));

        private static void Attach(ScrollViewer sv)
        {
            sv.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            sv.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
            sv.PreviewMouseMove += OnPreviewMouseMove;
            sv.LostMouseCapture += OnLostMouseCapture;
        }

        private static void Detach(ScrollViewer sv)
        {
            sv.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            sv.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
            sv.PreviewMouseMove -= OnPreviewMouseMove;
            sv.LostMouseCapture -= OnLostMouseCapture;
        }

        private static void OnPreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (sender is not ScrollViewer sv)
                return;

            // prevent scrollbar and its parts to handle the event
            if (e.OriginalSource is DependencyObject dep && IsOverScrollbar(dep))
                return;

            if (!sv.IsMouseOver)
                return;

            SetIsPanning(sv, true);
            SetPanStartPoint(sv, e.GetPosition(sv));
            SetPanStartOffset(sv, new Point(sv.HorizontalOffset, sv.VerticalOffset));
            // capture mouse so we continue to receive events while dragging
            sv.CaptureMouse();
            sv.Cursor = Cursors.Hand;

            e.Handled = true;
        }

        private static void OnPreviewMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            if (sender is not ScrollViewer sv)
                return;

            if (GetIsPanning(sv))
            {
                SetIsPanning(sv, false);
                sv.ReleaseMouseCapture();
                sv.Cursor = null;
                e.Handled = true;
            }
        }

        private static void OnLostMouseCapture(object? sender, MouseEventArgs e)
        {
            if (sender is not ScrollViewer sv)
                return;

            // ensure state is cleared and cursor restored if capture is lost unexpectedly
            if (GetIsPanning(sv))
            {
                SetIsPanning(sv, false);
                sv.Cursor = null;
            }
        }

        private static void OnPreviewMouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is not ScrollViewer sv)
                return;

            if (!GetIsPanning(sv))
                return;

            var currentPoint = e.GetPosition(sv);
            var start = GetPanStartPoint(sv);
            var delta = currentPoint - start;

            // invert delta so dragging the mouse moves the image in the expected direction
            var startOffset = GetPanStartOffset(sv);
            double newH = startOffset.X - delta.X;
            double newV = startOffset.Y - delta.Y;

            // clamp to scrollable extents
            newH = Math.Clamp(newH, 0, sv.ScrollableWidth);
            newV = Math.Clamp(newV, 0, sv.ScrollableHeight);

            sv.ScrollToHorizontalOffset(newH);
            sv.ScrollToVerticalOffset(newV);

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

        private static bool GetIsPanning(DependencyObject obj) => (bool)obj.GetValue(IsPanningProperty);
        private static void SetIsPanning(DependencyObject obj, bool value) => obj.SetValue(IsPanningProperty, value);

        private static Point GetPanStartPoint(DependencyObject obj) => (Point)obj.GetValue(PanStartPointProperty);
        private static void SetPanStartPoint(DependencyObject obj, Point value) => obj.SetValue(PanStartPointProperty, value);

        private static Point GetPanStartOffset(DependencyObject obj) => (Point)obj.GetValue(PanStartOffsetProperty);
        private static void SetPanStartOffset(DependencyObject obj, Point value) => obj.SetValue(PanStartOffsetProperty, value);
    }
}
