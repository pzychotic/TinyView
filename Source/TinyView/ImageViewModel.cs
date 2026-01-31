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
                OnPropertyChanged(nameof(ImageSizeText));
                OnPropertyChanged(nameof(ImageMinMaxText));
                OnPropertyChanged(nameof(ImageFormatText));
            }
        }

        public string WindowTitle => Filename.Length > 0 ? $"TinyView - {Filename}" : "TinyView";

        private string Filename = string.Empty;

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
        private string _valueText = "0,0: undefined";
        public string ValueText
        {
            get => _valueText;
            set { if (value == _valueText) return; _valueText = value; OnPropertyChanged(); }
        }

        public string ImageSizeText => RawData != null ? $"{RawData.Width}x{RawData.Height}" : "0x0";
        public string ImageMinMaxText => RawData != null ? $"{RawData.Min:0.##}..{RawData.Max:0.##}" : "0..0";
        public string ImageFormatText => RawData?.DataFormat ?? "undefined";

        // Selected color palette from the UI (nullable because initially none may be selected)
        private ColorPalettes.PaletteEntry _selectedPalette;
        public ColorPalettes.PaletteEntry SelectedPalette
        {
            get => _selectedPalette;
            set
            {
                if (Equals(value, _selectedPalette)) return;
                _selectedPalette = value;
                OnPropertyChanged();
                ApplyPalette();
            }
        }

        public ICommand OpenCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ZoomResetCommand { get; }
        public ICommand DropCommand { get; }
        public ICommand HoverCommand { get; }
        public ICommand LeaveHoverCommand { get; }

        public ImageViewModel()
        {
            OpenCommand = new RelayCommand<object?>(_ => ExecuteOpen());

            ZoomInCommand = new RelayCommand<object?>(_ => ZoomFactor *= 2.0);
            ZoomOutCommand = new RelayCommand<object?>(_ => ZoomFactor /= 2.0);
            ZoomResetCommand = new RelayCommand<object?>(_ => ZoomFactor = 1.0);

            DropCommand = new RelayCommand<string[]>(files =>
            {
                if (files != null && files.Length > 0)
                {
                    // only support one file right now
                    LoadImage(files[0]);
                }
            });

            HoverCommand = new RelayCommand<PixelPosition>(p =>
            {
                if (RawData != null)
                {
                    int x = p.X;
                    int y = p.Y;
                    if (x < 0 || y < 0 || x >= RawData.Width || y >= RawData.Height)
                    {
                        ValueText = "0,0: undefined";
                        return;
                    }

                    string? value = RawData.GetValueString(x, y);
                    ValueText = $"{x},{y}: {value}";
                }
            });

            LeaveHoverCommand = new RelayCommand<object?>(_ => { ValueText = "0,0: undefined"; });

            // set initial palette to first available entry
            if (ColorPalettes.Palettes.Count > 0)
            {
                SelectedPalette = ColorPalettes.Palettes[0];
            }
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

                OnPropertyChanged(nameof(WindowTitle));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Apply the currently-selected palette to the current RawData.
        /// </summary>
        public void ApplyPalette()
        {
            if (RawData == null)
                return;

            var bitmap = new WriteableBitmap(RawData.Width, RawData.Height, 96, 96, PixelFormats.Indexed8, SelectedPalette.Palette);
            bitmap.WritePixels(new Int32Rect(0, 0, RawData.Width, RawData.Height), RawData.IndexedData, RawData.Width, 0);
            ImageSource = bitmap;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
