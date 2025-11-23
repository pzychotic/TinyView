using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
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
            set { _rawData = value; OnPropertyChanged(); }
        }

        public string? Filename;

        public void LoadImage(string path)
        {
            try
            {
                var (raw, bmp) = MagickImageLoader.LoadImage(path);
                RawData = raw;
                ImageSource = bmp;

                Filename = Path.GetFileName(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
