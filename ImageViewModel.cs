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
            set { _rawData = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormatText)); }
        }

        public string? Filename;

        // Zoom handling
        private double _scaleFactor = 1.0;
        public double ScaleFactor
        {
            get => _scaleFactor;
            set
            {
                if (value == _scaleFactor) return;
                _scaleFactor = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ZoomText));
            }
        }

        public string ZoomText => $"Zoom: {ScaleFactor * 100.0}%";

        public string FormatText => RawData?.DataFormat ?? "Format: undefined";

        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }

        public ImageViewModel()
        {
            ZoomInCommand = new RelayCommand(_ => ScaleFactor *= 2.0);
            ZoomOutCommand = new RelayCommand(_ => ScaleFactor /= 2.0);
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
                OnPropertyChanged(nameof(FormatText));
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
