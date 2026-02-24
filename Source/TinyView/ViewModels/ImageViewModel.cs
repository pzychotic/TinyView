using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TinyView.Models;
using TinyView.Services;

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
                ZoomFactor = 1.0;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImageSizeText));
                OnPropertyChanged(nameof(ImageMinMaxText));
                OnPropertyChanged(nameof(ImageFormatText));
                ApplyPalette();
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

        // expose palettes to the view
        public IReadOnlyList<ColorPalettes.PaletteEntry> Palettes => ColorPalettes.Palettes;

        public ICommand OpenCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ZoomResetCommand { get; }
        public ICommand DropCommand { get; }
        public ICommand HoverCommand { get; }
        public ICommand LeaveHoverCommand { get; }

        // image loader services (instance-based)
        private readonly IImageLoader[] _imageLoaders;

        private readonly IDialogService _dialogService;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value) return;
                _isBusy = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ImageViewModel(IDialogService? dialogService = null)
        {
            _dialogService = dialogService ?? new NullDialogService();

            // initialize image loader implementations
            _imageLoaders = [new MagickImageLoader(), new PfimImageLoader(), new TiffImageLoader()];

            OpenCommand = new AsyncRelayCommand<object?>(async _ =>
            {
                const string filter = "Image Files (*.dds;*.png;*.tif;*.tiff)|*.dds;*.png;*.tif;*.tiff|DDS Files (*.dds)|*.dds|PNG Files (*.png)|*.png|TIFF Files (*.tif;*.tiff)|*.tif;*.tiff|All Files (*.*)|*.*";
                var path = _dialogService.ShowOpenFileDialog(filter);
                if (path != null)
                    await LoadImageAsync(path);
            }, _ => !IsBusy);

            AboutCommand = new RelayCommand<object?>(_ => _dialogService.ShowAbout());

            DropCommand = new AsyncRelayCommand<string[]>(async files =>
            {
                // only support one file right now
                if (files?.Length > 0)
                    await LoadImageAsync(files[0]);
            }, _ => !IsBusy);

            ExitCommand = new RelayCommand<object?>(_ => Application.Current.Shutdown());

            ZoomInCommand = new RelayCommand<object?>(_ => ZoomFactor *= 2.0);
            ZoomOutCommand = new RelayCommand<object?>(_ => ZoomFactor /= 2.0);
            ZoomResetCommand = new RelayCommand<object?>(_ => ZoomFactor = 1.0);

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

        private async Task LoadImageAsync(string path)
        {
            IsBusy = true;
            try
            {
                string ext = Path.GetExtension(path).ToLower();
                // pick a loader by asking each registered loader if it can handle the extension
                foreach (var loader in _imageLoaders)
                {
                    if (loader.CanLoad(ext))
                    {
                        RawData = await loader.LoadImageAsync(path);
                        break;
                    }
                }

                if (RawData == null)
                    throw new NotSupportedException($"Unsupported image format: {ext}");

                Filename = Path.GetFileName(path);
                OnPropertyChanged(nameof(WindowTitle));
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Error", $"Error loading image:\n{ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Apply the currently-selected palette to the current RawData.
        /// </summary>
        private void ApplyPalette()
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

        private sealed class NullDialogService : IDialogService
        {
            public string? ShowOpenFileDialog(string filter) => null;
            public void ShowError(string title, string message) { }
            public void ShowAbout() { }
        }
    }
}
