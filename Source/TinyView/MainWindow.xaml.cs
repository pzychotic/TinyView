using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using TinyView.ViewModels;

namespace TinyView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ImageViewModel _viewModel = new();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            // subscribe to mouse events on the Image control
            PreviewImage.MouseMove += PreviewImage_MouseMove;
            PreviewImage.MouseLeave += PreviewImage_MouseLeave;

            // handle Ctrl + MouseWheel for zooming
            PreviewMouseWheel += MainWindow_PreviewMouseWheel;

            // subscribe to panning events on the ScrollViewer and Image
            ImageScrollViewer.PreviewMouseLeftButtonDown += ImageScrollViewer_PreviewMouseLeftButtonDown;
            ImageScrollViewer.PreviewMouseLeftButtonUp += ImageScrollViewer_PreviewMouseLeftButtonUp;
            ImageScrollViewer.PreviewMouseMove += ImageScrollViewer_PreviewMouseMove;
            ImageScrollViewer.LostMouseCapture += ImageScrollViewer_LostMouseCapture;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(ImageViewModel.RawData))
            {
                // ensure we're on UI thread
                Dispatcher.Invoke(ApplyCurrentPalette);
            }
        }

        private void PreviewImage_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_viewModel.ImageSource == null || _viewModel.RawData == null)
                return;

            var pos = e.GetPosition(PreviewImage);
            var bmp = _viewModel.ImageSource;

            double displayWidth = PreviewImage.ActualWidth;
            double displayHeight = PreviewImage.ActualHeight;

            if (displayWidth <= 0 || displayHeight <= 0)
                return;

            int x = (int)(pos.X * bmp.PixelWidth / displayWidth);
            int y = (int)(pos.Y * bmp.PixelHeight / displayHeight);

            if (x < 0 || x >= bmp.PixelWidth || y < 0 || y >= bmp.PixelHeight)
            {
                _viewModel.ValueText = "0,0: undefined";
                return;
            }

            string? value = _viewModel.RawData.GetValueString(x, y);
            _viewModel.ValueText = $"{x},{y}: {value}";
        }

        private void PreviewImage_MouseLeave(object? sender, MouseEventArgs e) =>
            _viewModel.ValueText = "0,0: undefined";

        private void Image_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                // only support one file right now
                _viewModel.LoadImage(files[0]);
            }
        }

        private void ComboBoxColorPalette_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyCurrentPalette();
        }

        private void ApplyCurrentPalette()
        {
            if (ComboBoxColorPalette.SelectedItem is ColorPalettes.PaletteEntry entry)
            {
                _viewModel.ApplyPalette(entry.Palette);
            }
        }

        // accumulator to handle sub-notch (high-resolution) wheel deltas
        private double _wheelDeltaAccum = 0.0;

        // Ctrl + MouseWheel handler: zoom in/out by a small factor per wheel notch
        private void MainWindow_PreviewMouseWheel(object? sender, MouseWheelEventArgs e)
        {
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                return;

            const double zoomStep = 1.1; // 10% per notch
            const int wheelDeltaPerNotch = 120; // Win32 WHEEL_DELTA

            // accumulate delta to support high-resolution mice that send sub-notch values
            _wheelDeltaAccum += e.Delta;

            int wholeNotches = (int)(_wheelDeltaAccum / wheelDeltaPerNotch);
            if (wholeNotches != 0)
            {
                _viewModel.ZoomFactor *= Math.Pow(zoomStep, wholeNotches);
                // consume the notches we handled
                _wheelDeltaAccum -= wholeNotches * wheelDeltaPerNotch;
            }

            e.Handled = true;
        }

        // panning state
        private bool _isPanning = false;
        private Point _panStartPoint; // screen coords when pan started
        private Point _panStartOffset; // horizontal and vertical offset when pan started

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

        private void ImageScrollViewer_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
        {
            // if the click was on a scrollbar (or its parts) allow the scrollbar to handle it
            if (e.OriginalSource is DependencyObject dep && IsOverScrollbar(dep))
                return;

            if (ImageScrollViewer.IsMouseOver)
            {
                _isPanning = true;
                _panStartPoint = e.GetPosition(this);
                _panStartOffset = new Point(ImageScrollViewer.HorizontalOffset, ImageScrollViewer.VerticalOffset);
                // capture mouse so we continue to receive events while dragging
                ImageScrollViewer.CaptureMouse();
                // show hand cursor while panning
                ImageScrollViewer.Cursor = Cursors.Hand;
                e.Handled = true;
            }
        }

        private void ImageScrollViewer_PreviewMouseLeftButtonUp(object? sender, MouseButtonEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                ImageScrollViewer.ReleaseMouseCapture();
                // restore default cursor
                ImageScrollViewer.Cursor = null;
                e.Handled = true;
            }
        }

        private void ImageScrollViewer_LostMouseCapture(object? sender, MouseEventArgs e)
        {
            // ensure state is cleared and cursor restored if capture is lost unexpectedly
            if (_isPanning)
            {
                _isPanning = false;
                ImageScrollViewer.Cursor = null;
            }
        }

        private void ImageScrollViewer_PreviewMouseMove(object? sender, MouseEventArgs e)
        {
            if (!_isPanning)
                return;

            var currentPoint = e.GetPosition(this);
            var delta = currentPoint - _panStartPoint;

            // invert delta so dragging the mouse moves the image in the expected direction
            double newH = _panStartOffset.X - delta.X;
            double newV = _panStartOffset.Y - delta.Y;

            // clamp to scrollable extents
            newH = Math.Clamp(newH, 0, ImageScrollViewer.ScrollableWidth);
            newV = Math.Clamp(newV, 0, ImageScrollViewer.ScrollableHeight);

            ImageScrollViewer.ScrollToHorizontalOffset(newH);
            ImageScrollViewer.ScrollToVerticalOffset(newV);

            e.Handled = true;
        }
    }
}
