using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TinyView.Models;
using TinyView.Services;

namespace TinyView.ViewModels;

public partial class ImageViewModel : ObservableObject
{
    [ObservableProperty]
    private WriteableBitmap? _imageSource;

    /// <summary>
    /// Notifier that view-layer behaviors subscribe to in order to reset
    /// viewport state (cancel panning, re-center content, etc.).
    /// </summary>
    public ViewportResetNotifier PanResetNotifier { get; } = new();

    /// <summary>
    /// Whether the region-select tool is currently active (toggle button state).
    /// </summary>
    [ObservableProperty]
    private bool _isRegionSelectActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage))]
    [NotifyPropertyChangedFor(nameof(ImageSizeText))]
    [NotifyPropertyChangedFor(nameof(ImageMinMaxText))]
    [NotifyPropertyChangedFor(nameof(ImageFormatText))]
    private IRawImageDataProvider? _rawData;

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

    // Display range for normalization (editable by the user via toolbar)
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ResetMinMaxCommand))]
    private float _displayMin;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ResetMinMaxCommand))]
    private float _displayMax;

    private bool _suppressDisplayRangeUpdates;

    /// <summary>
    /// Sets both <see cref="DisplayMin"/> and <see cref="DisplayMax"/> in a
    /// single batch, re-normalizing and re-rendering only once.
    /// </summary>
    private void SetDisplayRange(float min, float max)
    {
        _suppressDisplayRangeUpdates = true;
        DisplayMin = min;
        DisplayMax = max;
        _suppressDisplayRangeUpdates = false;
        ApplyDisplayRange();
        ResetMinMaxCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Sets <see cref="DisplayMin"/> and <see cref="DisplayMax"/> to the
    /// original min/max of the current image without triggering re-normalization.
    /// Called once when a new image is loaded.
    /// </summary>
    private void InitializeDisplayRange()
    {
        _suppressDisplayRangeUpdates = true;
        DisplayMin = RawData?.Min ?? 0;
        DisplayMax = RawData?.Max ?? 0;
        _suppressDisplayRangeUpdates = false;
    }

    private bool CanResetMinMax() => HasImage && (DisplayMin != RawData!.Min || DisplayMax != RawData!.Max);

    /// <summary>
    /// Resets <see cref="DisplayMin"/> and <see cref="DisplayMax"/> to the
    /// original values computed from the loaded image data.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanResetMinMax))]
    private void ResetMinMax() => SetDisplayRange(RawData!.Min, RawData!.Max);

    /// <summary>
    /// Re-normalizes the raw data with the current display range and re-applies the palette.
    /// </summary>
    private void ApplyDisplayRange()
    {
        if (RawData == null)
            return;

        RawData.RegenerateIndexedData(DisplayMin, DisplayMax);
        ApplyPalette();
    }

    private bool CanToggleRegionSelect() => HasImage;

    [RelayCommand(CanExecute = nameof(CanToggleRegionSelect))]
    private void ToggleRegionSelect()
    {
        IsRegionSelectActive = !IsRegionSelectActive;
    }

    // Flip state (toggled via toolbar/menu, applied via LayoutTransform)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FlipScaleX))]
    private bool _isFlippedHorizontally;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FlipScaleY))]
    private bool _isFlippedVertically;

    public double FlipScaleX => IsFlippedHorizontally ? -1.0 : 1.0;
    public double FlipScaleY => IsFlippedVertically ? -1.0 : 1.0;

    // Selected color palette from the UI (nullable because initially none may be selected)
    [ObservableProperty]
    private ColorPalettes.PaletteEntry _selectedPalette;

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

    partial void OnRawDataChanged(IRawImageDataProvider? value)
    {
        if (value == null)
            return;

        Zoom.Reset();
        IsFlippedHorizontally = false;
        IsFlippedVertically = false;
        IsRegionSelectActive = false;
        PanResetNotifier.RequestReset();
        InitializeDisplayRange();
        ApplyPalette();
        ZoomInCommand.NotifyCanExecuteChanged();
        ZoomOutCommand.NotifyCanExecuteChanged();
        ZoomResetCommand.NotifyCanExecuteChanged();
        ResetMinMaxCommand.NotifyCanExecuteChanged();
        ToggleRegionSelectCommand.NotifyCanExecuteChanged();
    }

    partial void OnDisplayMinChanged(float value)
    {
        if (!_suppressDisplayRangeUpdates)
            ApplyDisplayRange();
    }

    partial void OnDisplayMaxChanged(float value)
    {
        if (!_suppressDisplayRangeUpdates)
            ApplyDisplayRange();
    }

    partial void OnSelectedPaletteChanged(ColorPalettes.PaletteEntry value) => ApplyPalette();

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

    /// <summary>
    /// Resets zoom to 1× and re-centers the viewport.
    /// Ready to bind to a toolbar/menu button.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanZoomReset))]
    private void ResetView()
    {
        Zoom.Reset();
        PanResetNotifier.RequestReset();
    }

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

    /// <summary>
    /// Computes the min/max raw pixel values within the selected region
    /// and applies them as the new display range.
    /// </summary>
    [RelayCommand]
    private void ApplyRegionMinMax(PixelRect rect)
    {
        if (RawData == null || rect.Width <= 0 || rect.Height <= 0)
            return;

        var (min, max) = RawData.GetRegionMinMax(rect.X, rect.Y, rect.Width, rect.Height);
        SetDisplayRange(min, max);
    }

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
        ImageSource = bitmap;
    }

    private sealed class NullDialogService : IDialogService
    {
        public string? ShowOpenFileDialog(string filter) => null;
        public void ShowError(string title, string message) { }
        public void ShowAbout() { }
        public void RequestShutdown() { }
    }
}
