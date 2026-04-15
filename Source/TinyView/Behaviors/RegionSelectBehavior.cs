using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TinyView.ViewModels;

namespace TinyView.Behaviors
{
    /// <summary>
    /// Allows the user to draw a rectangular selection on an <see cref="Image"/>
    /// using left-mouse-button drag. The selection is expressed in image-pixel
    /// coordinates and a command is invoked with a <see cref="PixelRect"/> on completion.
    /// A dashed rectangle overlay is shown in the image's coordinate space.
    /// </summary>
    public class RegionSelectBehavior : Behavior<Image>
    {
        #region Dependency Properties

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(RegionSelectBehavior),
                new FrameworkPropertyMetadata(false,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsActiveChanged));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public static readonly DependencyProperty SelectionCommandProperty =
            DependencyProperty.Register(
                nameof(SelectionCommand),
                typeof(ICommand),
                typeof(RegionSelectBehavior),
                new PropertyMetadata(null));

        public ICommand? SelectionCommand
        {
            get => (ICommand?)GetValue(SelectionCommandProperty);
            set => SetValue(SelectionCommandProperty, value);
        }

        #endregion

        private bool _isDragging;
        private Point _startPixel;
        private Rectangle? _selectionRect;
        private Canvas? _overlayCanvas;
        private Window? _parentWindow;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonDown += OnMouseLeftButtonDown;
            AssociatedObject.AddHandler(UIElement.MouseMoveEvent, new MouseEventHandler(OnMouseMove), handledEventsToo: true);
            AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            AssociatedObject.RemoveHandler(UIElement.MouseMoveEvent, new MouseEventHandler(OnMouseMove));
            AssociatedObject.MouseLeftButtonUp -= OnMouseLeftButtonUp;

            UnsubscribeEscapeKey();
            RemoveOverlay();
            base.OnDetaching();
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RegionSelectBehavior behavior)
                return;

            if ((bool)e.NewValue)
            {
                behavior.AssociatedObject.Cursor = Cursors.Cross;
                behavior.SubscribeEscapeKey();
            }
            else
            {
                behavior.CancelSelection();
                behavior.AssociatedObject.Cursor = null;
                behavior.RemoveOverlay();
                behavior.UnsubscribeEscapeKey();
            }
        }

        #region Mouse Handling

        private void OnMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            if (!IsActive)
                return;

            var pixel = GetPixelPosition(e);
            if (pixel == null)
                return;

            // Remove any previous selection overlay
            RemoveOverlay();

            _startPixel = pixel.Value;
            _isDragging = true;
            AssociatedObject.CaptureMouse();

            EnsureOverlay();

            e.Handled = true;
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isDragging)
                return;

            var pixel = GetPixelPosition(e);
            if (pixel == null)
                return;

            UpdateSelectionRect(_startPixel, pixel.Value);
            e.Handled = true;
        }

        private void OnMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            if (!_isDragging)
                return;

            _isDragging = false;
            AssociatedObject.ReleaseMouseCapture();

            var pixel = GetPixelPosition(e);
            if (pixel == null)
                return;

            var rect = MakePixelRect(_startPixel, pixel.Value);
            if (rect.Width > 0 && rect.Height > 0)
            {
                UpdateSelectionRect(_startPixel, pixel.Value);

                if (SelectionCommand is { } cmd && cmd.CanExecute(rect))
                    cmd.Execute(rect);
            }
            else
            {
                RemoveOverlay();
            }

            e.Handled = true;
        }

        #endregion

        #region Escape Key

        private void SubscribeEscapeKey()
        {
            _parentWindow = Window.GetWindow(AssociatedObject);
            if (_parentWindow != null)
                _parentWindow.PreviewKeyDown += OnPreviewKeyDown;
        }

        private void UnsubscribeEscapeKey()
        {
            if (_parentWindow != null)
            {
                _parentWindow.PreviewKeyDown -= OnPreviewKeyDown;
                _parentWindow = null;
            }
        }

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && IsActive)
            {
                IsActive = false; // two-way binding updates the ViewModel
                e.Handled = true;
            }
        }

        #endregion

        #region Overlay Management

        private void EnsureOverlay()
        {
            if (_overlayCanvas != null)
                return;

            // The Image must be inside a Panel (Grid) for us to add a sibling Canvas
            if (AssociatedObject.Parent is not Panel parent)
                return;

            _overlayCanvas = new Canvas
            {
                IsHitTestVisible = false
            };

            _selectionRect = new Rectangle
            {
                Stroke = Brushes.Lime,
                StrokeThickness = 1,
                StrokeDashArray = [4, 2],
                Visibility = Visibility.Collapsed
            };

            _overlayCanvas.Children.Add(_selectionRect);
            parent.Children.Add(_overlayCanvas);
        }

        private void RemoveOverlay()
        {
            if (_overlayCanvas != null)
            {
                if (_overlayCanvas.Parent is Panel parent)
                    parent.Children.Remove(_overlayCanvas);

                _selectionRect = null;
                _overlayCanvas = null;
            }
        }

        private void UpdateSelectionRect(Point start, Point end)
        {
            if (_selectionRect == null)
                return;

            var rect = MakePixelRect(start, end);

            Canvas.SetLeft(_selectionRect, rect.X);
            Canvas.SetTop(_selectionRect, rect.Y);
            _selectionRect.Width = rect.Width;
            _selectionRect.Height = rect.Height;
            _selectionRect.Visibility = Visibility.Visible;
        }

        #endregion

        #region Coordinate Helpers

        /// <summary>
        /// Converts the mouse position to image-pixel coordinates, or null if
        /// the image has no source / the position is out of bounds.
        /// </summary>
        private Point? GetPixelPosition(MouseEventArgs e)
        {
            if (AssociatedObject.Source is not BitmapSource bmp)
                return null;

            double displayWidth = AssociatedObject.ActualWidth;
            double displayHeight = AssociatedObject.ActualHeight;
            if (displayWidth <= 0 || displayHeight <= 0)
                return null;

            var pos = e.GetPosition(AssociatedObject);
            double px = pos.X * bmp.PixelWidth / displayWidth;
            double py = pos.Y * bmp.PixelHeight / displayHeight;

            // Clamp to image bounds
            px = Math.Clamp(px, 0, bmp.PixelWidth);
            py = Math.Clamp(py, 0, bmp.PixelHeight);

            return new Point(px, py);
        }

        private static PixelRect MakePixelRect(Point a, Point b)
        {
            int x = (int)Math.Min(a.X, b.X);
            int y = (int)Math.Min(a.Y, b.Y);
            int x2 = (int)Math.Max(a.X, b.X);
            int y2 = (int)Math.Max(a.Y, b.Y);
            return new PixelRect(x, y, x2 - x, y2 - y);
        }

        private void CancelSelection()
        {
            if (_isDragging)
            {
                _isDragging = false;
                AssociatedObject.ReleaseMouseCapture();
            }
        }

        #endregion
    }
}
