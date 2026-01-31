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

        // Selection is now bound to the view-model via SelectedPalette; keep helper for compatibility
        private void ApplyCurrentPalette()
        {
            _viewModel.ApplyPalette(_viewModel.SelectedPalette.Palette);
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
