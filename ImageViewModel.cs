using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TinyView.ViewModels
{
    public class ImageViewModel : INotifyPropertyChanged
    {
        private WriteableBitmap? _imageSource;
        public WriteableBitmap? ImageSource
        {
            get => _imageSource;
            set { _imageSource = value; OnPropertyChanged(); }
        }

        private IRawImageDataProvider? _rawData;
        public IRawImageDataProvider? RawData
        {
            get => _rawData;
            set
            {
                _rawData = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormatText));
                OnPropertyChanged(nameof(ImageSizeText));
            }
        }

        public string? Filename;

        // Zoom handling
        private double _zoomFactor = 1.0;
        public double ZoomFactor
        {
            get => _zoomFactor;
            set
            {
                if (value == _zoomFactor) return;
                _zoomFactor = value;
                OnPropertyChanged();
            }
        }

        // Status text shown in the status bar (pixel position and value)
        private string _valueText = "Pos: 0,0 - Value: undefined";
        public string ValueText
        {
            get => _valueText;
            set { if (value == _valueText) return; _valueText = value; OnPropertyChanged(); }
        }

        public string ImageSizeText => RawData != null ? $"Size: {RawData.Width}x{RawData.Height}" : "Size: 0x0";
        public string FormatText => RawData?.DataFormat ?? "Format: undefined";

        public ICommand OpenCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }

        public event EventHandler? ImageLoaded;

        public ImageViewModel()
        {
            OpenCommand = new RelayCommand(_ => ExecuteOpen());
            ZoomInCommand = new RelayCommand(_ => ZoomFactor *= 2.0);
            ZoomOutCommand = new RelayCommand(_ => ZoomFactor /= 2.0);
        }

        private void ExecuteOpen()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files (*.dds;*.png)|*.dds;*.png|PNG Files (*.png)|*.png|DDS Files (*.dds)|*.dds|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadImage(dialog.FileName);
            }
        }

        public void LoadImage(string path)
        {
            try
            {
                string ext = Path.GetExtension(path).ToLower();
                if (ext == ".png")
                {
                    RawData = MagickImageLoader.LoadImage(path);
                }
                else if (ext == ".dds")
                {
                    RawData = PfimImageLoader.LoadImage(path);
                }
                else
                {
                    throw new NotSupportedException($"Unsupported image format: {ext}");
                }

                Filename = Path.GetFileName(path);

                // notify listeners that an image was loaded
                ImageLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ApplyPalette(BitmapPalette palette)
        {
            if (RawData == null)
                return;

            var bitmap = new WriteableBitmap(RawData.Width, RawData.Height, 96, 96, PixelFormats.Indexed8, palette);
            bitmap.WritePixels(new Int32Rect(0, 0, RawData.Width, RawData.Height), RawData.IndexedData, RawData.Width, 0);
            ImageSource = bitmap;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
