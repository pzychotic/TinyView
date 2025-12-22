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

            _viewModel.ImageLoaded += ViewModel_ImageLoaded;

            // subscribe to mouse events on the Image control
            PreviewImage.MouseMove += PreviewImage_MouseMove;
            PreviewImage.MouseLeave += PreviewImage_MouseLeave;
        }

        private void ViewModel_ImageLoaded(object? sender, System.EventArgs e)
        {
            Title = $"TinyView - {_viewModel.Filename}";
            ApplyCurrentPalette();
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
                _viewModel.ValueText = "Pos: 0,0 - Value: undefined";
                return;
            }

            string? value = _viewModel.RawData.GetValueString(x, y);
            _viewModel.ValueText = $"Pos: {x},{y} - Value: {value}";
        }

        private void PreviewImage_MouseLeave(object? sender, MouseEventArgs e)
        {
            _viewModel.ValueText = "Pos: 0,0 - Value: undefined";
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
