using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    }
}
