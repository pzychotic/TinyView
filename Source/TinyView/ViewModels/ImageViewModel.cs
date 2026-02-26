using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TinyView.Models;
using TinyView.Services;

namespace TinyView.ViewModels
{
    public partial class ImageViewModel : ObservableObject
    {
        [ObservableProperty]
        private WriteableBitmap? _imageSource;

        /// <summary>
        /// Toggled each time the pan state should be reset (e.g. when a new image is loaded).
        /// Bound to ScrollViewerPanBehavior.ResetTrigger.
        /// </summary>
        private bool _panResetTrigger;
        public bool PanResetTrigger
        {
            get => _panResetTrigger;
            set => SetProperty(ref _panResetTrigger, value);
        }

        private IRawImageDataProvider? _rawData;
        public IRawImageDataProvider? RawData
        {
            get => _rawData;
            set
            {
                if (SetProperty(ref _rawData, value))
                {
                    Zoom.Reset();
                    PanResetTrigger = !PanResetTrigger;
                    OnPropertyChanged(nameof(HasImage));
                    OnPropertyChanged(nameof(ImageSizeText));
                    OnPropertyChanged(nameof(ImageMinMaxText));
                    OnPropertyChanged(nameof(ImageFormatText));
                    ApplyPalette();
                    ZoomInCommand.NotifyCanExecuteChanged();
                    ZoomOutCommand.NotifyCanExecuteChanged();
                    ZoomResetCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string WindowTitle => Filename.Length > 0 ? $"TinyView - {Filename}" : "TinyView";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WindowTitle))]
        private string _filename = string.Empty;

        public ZoomState Zoom { get; } = new ZoomState();

        // Status text shown in the status bar (pixel position and value)
        [ObservableProperty]
        private string _valueText = "0,0: undefined";

        public bool HasImage => RawData != null;
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
                if (SetProperty(ref _selectedPalette, value))
                {
                    ApplyPalette();
                }
            }
        }

        // expose palettes to the view
        public IReadOnlyList<ColorPalettes.PaletteEntry> Palettes => ColorPalettes.Palettes;

        // image loader services (instance-based)
        private readonly IImageLoader[] _imageLoaders;

        private readonly IDialogService _dialogService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor("OpenCommand")]
        [NotifyCanExecuteChangedFor("DropCommand")]
        private bool _isBusy;

        public ImageViewModel(IDialogService? dialogService = null)
        {
            _dialogService = dialogService ?? new NullDialogService();

            // initialize image loader implementations
            _imageLoaders = [new MagickImageLoader(), new PfimImageLoader(), new TiffImageLoader()];

            Zoom.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(ZoomState.CanZoomIn) or nameof(ZoomState.CanZoomOut))
                {
                    ZoomInCommand.NotifyCanExecuteChanged();
                    ZoomOutCommand.NotifyCanExecuteChanged();
                }
            };

            // set initial palette to first available entry
            if (ColorPalettes.Palettes.Count > 0)
            {
                SelectedPalette = ColorPalettes.Palettes[0];
            }
        }

        private bool CanExecuteWhenNotBusy() => !IsBusy;

        [RelayCommand(CanExecute = nameof(CanExecuteWhenNotBusy))]
        private async Task OpenAsync()
        {
            const string filter = "Image Files (*.dds;*.png;*.tif;*.tiff)|*.dds;*.png;*.tif;*.tiff|DDS Files (*.dds)|*.dds|PNG Files (*.png)|*.png|TIFF Files (*.tif;*.tiff)|*.tif;*.tiff|All Files (*.*)|*.*";
            var path = _dialogService.ShowOpenFileDialog(filter);
            if (path != null)
                await LoadImageAsync(path);
        }

        [RelayCommand]
        private void About() => _dialogService.ShowAbout();

        [RelayCommand(CanExecute = nameof(CanExecuteWhenNotBusy))]
        private async Task DropAsync(string[]? files)
        {
            // only support one file right now
            if (files?.Length > 0)
                await LoadImageAsync(files[0]);
        }

        [RelayCommand]
        private void Exit() => _dialogService.RequestShutdown();

        private bool CanZoomIn() => HasImage && Zoom.CanZoomIn;
        private bool CanZoomOut() => HasImage && Zoom.CanZoomOut;
        private bool CanZoomReset() => HasImage;

        [RelayCommand(CanExecute = nameof(CanZoomIn))]
        private void ZoomIn() => Zoom.ZoomIn();

        [RelayCommand(CanExecute = nameof(CanZoomOut))]
        private void ZoomOut() => Zoom.ZoomOut();

        [RelayCommand(CanExecute = nameof(CanZoomReset))]
        private void ZoomReset() => Zoom.Reset();

        [RelayCommand]
        private void Hover(PixelPosition p)
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
        }

        [RelayCommand]
        private void LeaveHover() => ValueText = "0,0: undefined";

        private async Task LoadImageAsync(string path)
        {
            IsBusy = true;
            try
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
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
            this.ImageSource = bitmap;
        }

        private sealed class NullDialogService : IDialogService
        {
            public string? ShowOpenFileDialog(string filter) => null;
            public void ShowError(string title, string message) { }
            public void ShowAbout() { }
            public void RequestShutdown() { }
        }
    }
}
