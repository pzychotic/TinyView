using System.ComponentModel;
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

        private ushort[,]? _rawData;
        public ushort[,]? RawData
        {
            get => _rawData;
            set { _rawData = value; OnPropertyChanged(); }
        }

        public void LoadImage(string path)
        {
            try
            {
                var (raw, bmp) = MagickImageLoader.LoadImage(path);
                RawData = raw;
                ImageSource = bmp;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
