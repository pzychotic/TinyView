using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TinyView.ViewModels;
using System.Windows.Controls;

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

            // subscribe to mouse events on the Image control
            PreviewImage.MouseMove += PreviewImage_MouseMove;
            PreviewImage.MouseLeave += PreviewImage_MouseLeave;
        }

        private void FileOpen_Executed(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files (*.dds;*.png)|*.dds;*.png|PNG Files (*.png)|*.png|DDS Files (*.dds)|*.dds|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                _viewModel.LoadImage(dialog.FileName);
                Title = $"TinyView - {_viewModel.Filename}";

                ApplyCurrentPalette();
            }
        }

        private void PreviewImage_MouseMove(object? sender, MouseEventArgs e)
        {
            var vm = DataContext as ImageViewModel;
            if (vm?.ImageSource == null || vm?.RawData == null)
                return;

            var pos = e.GetPosition(PreviewImage);
            var bmp = vm.ImageSource;

            double displayWidth = PreviewImage.ActualWidth;
            double displayHeight = PreviewImage.ActualHeight;

            if (displayWidth <= 0 || displayHeight <= 0)
                return;

            int x = (int)(pos.X * bmp.PixelWidth / displayWidth);
            int y = (int)(pos.Y * bmp.PixelHeight / displayHeight);

            if (x < 0 || x >= bmp.PixelWidth || y < 0 || y >= bmp.PixelHeight)
            {
                LabelValue.Text = "Pos: 0,0 - Value: undefined";
                return;
            }

            string? value = vm.RawData.GetValueString(x, y);
            LabelValue.Text = $"Pos: {x},{y} - Value: {value}";
        }

        private void PreviewImage_MouseLeave(object? sender, MouseEventArgs e)
        {
            LabelValue.Text = "Pos: 0,0 - Value: undefined";
        }

        private double _scaleFactor = 1.0;

        private void ButtonZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _scaleFactor /= 2.0;
            PreviewImage.LayoutTransform = new ScaleTransform(_scaleFactor, _scaleFactor);
            LabelZoom.Text = string.Format("Zoom: {0}%", _scaleFactor * 100.0);
        }

        private void ButtonZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _scaleFactor *= 2.0;
            PreviewImage.LayoutTransform = new ScaleTransform(_scaleFactor, _scaleFactor);
            LabelZoom.Text = string.Format("Zoom: {0}%", _scaleFactor * 100.0);
        }

        private void Image_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 0)
                    return;
                // only support one file right now
                _viewModel.LoadImage(files[0]);
                Title = $"TinyView - {_viewModel.Filename}";

                ApplyCurrentPalette();
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
    }
}
