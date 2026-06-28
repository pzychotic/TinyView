using TinyView.Models;
using TinyView.Services;
using TinyView.ViewModels;

namespace TinyView.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class ImageViewModelTests
{
    [Test]
    public void ZoomCommands_UpdateZoomFactor()
    {
        var vm = new ImageViewModel();
        vm.RawData = new RawImageData<int>(1, 1, new int[1], "INT_FMT");
        vm.Zoom.Factor = 1.0;

        vm.ZoomInCommand.Execute(null);
        Assert.That(vm.Zoom.Factor, Is.EqualTo(2.0));

        vm.ZoomOutCommand.Execute(null);
        vm.ZoomOutCommand.Execute(null);
        Assert.That(vm.Zoom.Factor, Is.EqualTo(0.5));

        vm.Zoom.Factor = 4.0;
        vm.ZoomResetCommand.Execute(null);
        Assert.That(vm.Zoom.Factor, Is.EqualTo(1.0));
    }

    [Test]
    public void HoverCommand_SetsValueText_ForInRangeAndOutOfRange()
    {
        int width = 2, height = 2;
        var data = new int[width * height];
        data[0 * width + 0] = 0;
        data[0 * width + 1] = 255;
        data[1 * width + 0] = 128;
        data[1 * width + 1] = 64;

        var provider = new RawImageData<int>(width, height, data, "INT_FMT");
        var vm = new ImageViewModel();
        vm.RawData = provider;

        // in-range
        vm.HoverCommand.Execute(new PixelPosition(1, 0));
        Assert.That(vm.ValueText, Is.EqualTo("1,0: 255"));

        // out-of-range should set undefined
        vm.HoverCommand.Execute(new PixelPosition(5, 5));
        Assert.That(vm.ValueText, Is.EqualTo("0,0: undefined"));
    }

    [Test]
    public void LeaveHoverCommand_ResetsValueText()
    {
        var vm = new ImageViewModel();
        vm.ValueText = "10,10: 123";
        vm.LeaveHoverCommand.Execute(null);
        Assert.That(vm.ValueText, Is.EqualTo("0,0: undefined"));
    }

    [Test]
    public void ApplyPalette_SetsImageSource_FromRawDataIndexedBytes()
    {
        int width = 2, height = 2;
        var data = new int[width * height];
        data[0 * width + 0] = 0;
        data[0 * width + 1] = 255;
        data[1 * width + 0] = 128;
        data[1 * width + 1] = 64;

        var provider = new RawImageData<int>(width, height, data, "INT_FMT");

        var vm = new ImageViewModel();
        vm.RawData = provider;

        // ensure a palette is selected (constructor picks first if available)
        vm.SelectedPalette = vm.Palettes[1];

        // ApplyPalette should have been called by SelectedPalette setter
        Assert.That(vm.ImageSource, Is.Not.Null);
        Assert.That(vm.ImageSource!.PixelWidth, Is.EqualTo(width));
        Assert.That(vm.ImageSource.PixelHeight, Is.EqualTo(height));

        var buffer = new byte[width * height];
        vm.ImageSource.CopyPixels(buffer, width, 0);

        Assert.That(buffer, Is.EqualTo(provider.IndexedData));
    }

    [Test]
    public void RawData_Set_RaisesExpectedPropertyChangedEvents()
    {
        int width = 1, height = 1;
        var data = new int[width * height];
        data[0 * width + 0] = 7;
        var provider = new RawImageData<int>(width, height, data, "INT_FMT");

        var vm = new ImageViewModel();
        var seen = new List<string>();
        vm.PropertyChanged += (s, e) => seen.Add(e.PropertyName ?? string.Empty);

        vm.RawData = provider;

        // RawData setter notifies these properties
        Assert.That(seen, Does.Contain("RawData"));
        Assert.That(seen, Does.Contain("ImageSizeText"));
        Assert.That(seen, Does.Contain("ImageMinMaxText"));
        Assert.That(seen, Does.Contain("ImageFormatText"));
    }

    [Test]
    public void HasImage_IsFalseByDefault_TrueAfterSettingRawData()
    {
        var vm = new ImageViewModel();
        Assert.That(vm.HasImage, Is.False);

        vm.RawData = new RawImageData<int>(1, 1, new int[1], "INT_FMT");
        Assert.That(vm.HasImage, Is.True);
    }

    [Test]
    public void Filename_SetUpdatesWindowTitle()
    {
        var vm = new ImageViewModel();
        Assert.That(vm.WindowTitle, Is.EqualTo("TinyView"));

        vm.Filename = "test.png";
        Assert.That(vm.WindowTitle, Is.EqualTo("TinyView - test.png"));
    }

    [Test]
    public void ImageSizeText_ReflectsRawData()
    {
        var vm = new ImageViewModel();
        Assert.That(vm.ImageSizeText, Is.EqualTo("0x0"));

        vm.RawData = new RawImageData<int>(4, 3, new int[12], "INT_FMT");
        Assert.That(vm.ImageSizeText, Is.EqualTo("4x3"));
    }

    [Test]
    public void ImageFormatText_ReflectsRawData()
    {
        var vm = new ImageViewModel();
        Assert.That(vm.ImageFormatText, Is.EqualTo("undefined"));

        vm.RawData = new RawImageData<int>(1, 1, new int[1], "MY_FMT");
        Assert.That(vm.ImageFormatText, Is.EqualTo("MY_FMT"));
    }

    [Test]
    public void PanResetNotifier_FiresWhenRawDataChanges()
    {
        var vm = new ImageViewModel();
        bool fired = false;
        vm.PanResetNotifier.ResetRequested += () => fired = true;

        vm.RawData = new RawImageData<int>(1, 1, new int[1], "INT_FMT");
        Assert.That(fired, Is.True);
    }

    [Test]
    public void ExitCommand_InvokesDialogServiceShutdown()
    {
        var svc = new SpyDialogService();
        var vm = new ImageViewModel(svc);
        vm.ExitCommand.Execute(null);
        Assert.That(svc.ShutdownRequested, Is.True);
    }

    [Test]
    public void AboutCommand_InvokesDialogServiceShowAbout()
    {
        var svc = new SpyDialogService();
        var vm = new ImageViewModel(svc);
        vm.AboutCommand.Execute(null);
        Assert.That(svc.AboutShown, Is.True);
    }

    [Test]
    public void IsBusy_DisablesOpenAndDropCommands()
    {
        var vm = new ImageViewModel();
        Assert.That(vm.OpenCommand.CanExecute(null), Is.True);
        Assert.That(vm.DropCommand.CanExecute(null), Is.True);

        vm.IsBusy = true;
        Assert.That(vm.OpenCommand.CanExecute(null), Is.False);
        Assert.That(vm.DropCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void ZoomCommands_DisabledWhenNoImage()
    {
        var vm = new ImageViewModel();
        Assert.That(vm.ZoomInCommand.CanExecute(null), Is.False);
        Assert.That(vm.ZoomOutCommand.CanExecute(null), Is.False);
        Assert.That(vm.ZoomResetCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void DisplayMinMax_InitializedFromRawData()
    {
        var data = new float[] { -5f, 0f, 10f, 20f };
        var provider = new RawImageData<float>(2, 2, data, "FLT_FMT");

        var vm = new ImageViewModel();
        vm.RawData = provider;

        Assert.That(vm.DisplayMin, Is.EqualTo(provider.Min));
        Assert.That(vm.DisplayMax, Is.EqualTo(provider.Max));
    }

    [Test]
    public void DisplayMinMax_RaisesPropertyChanged_WhenRawDataSet()
    {
        var data = new float[] { 1f, 2f, 3f, 4f };
        var provider = new RawImageData<float>(2, 2, data, "FLT_FMT");

        var vm = new ImageViewModel();
        var seen = new List<string>();
        vm.PropertyChanged += (_, e) => seen.Add(e.PropertyName ?? string.Empty);

        vm.RawData = provider;

        Assert.That(seen, Does.Contain("DisplayMin"));
        Assert.That(seen, Does.Contain("DisplayMax"));
    }

    [Test]
    public void DisplayMin_Change_RegeneratesIndexedDataAndImage()
    {
        var data = new float[] { 0f, 50f, 100f, 200f };
        var provider = new RawImageData<float>(2, 2, data, "FLT_FMT");

        var vm = new ImageViewModel();
        vm.RawData = provider;

        var originalIndexed = (byte[])provider.IndexedData.Clone();

        // changing DisplayMin should re-normalize
        vm.DisplayMin = 50f;

        Assert.That(provider.IndexedData, Is.Not.EqualTo(originalIndexed));
        Assert.That(vm.ImageSource, Is.Not.Null);
    }

    [Test]
    public void DisplayMax_Change_RegeneratesIndexedDataAndImage()
    {
        var data = new float[] { 0f, 50f, 100f, 200f };
        var provider = new RawImageData<float>(2, 2, data, "FLT_FMT");

        var vm = new ImageViewModel();
        vm.RawData = provider;

        var originalIndexed = (byte[])provider.IndexedData.Clone();

        vm.DisplayMax = 100f;

        Assert.That(provider.IndexedData, Is.Not.EqualTo(originalIndexed));
        Assert.That(vm.ImageSource, Is.Not.Null);
    }

    [Test]
    public void ResetMinMaxCommand_RevertsToOriginalValues()
    {
        var data = new float[] { 0f, 50f, 100f, 200f };
        var provider = new RawImageData<float>(2, 2, data, "FLT_FMT");

        var vm = new ImageViewModel();
        vm.RawData = provider;

        // change display range
        vm.DisplayMin = 25f;
        vm.DisplayMax = 150f;
        Assert.That(vm.ResetMinMaxCommand.CanExecute(null), Is.True);

        // reset
        vm.ResetMinMaxCommand.Execute(null);

        Assert.That(vm.DisplayMin, Is.EqualTo(provider.Min));
        Assert.That(vm.DisplayMax, Is.EqualTo(provider.Max));
        Assert.That(vm.ResetMinMaxCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void ResetMinMaxCommand_DisabledWhenNoImage()
    {
        var vm = new ImageViewModel();
        Assert.That(vm.ResetMinMaxCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void ResetMinMaxCommand_DisabledWhenValuesMatchOriginal()
    {
        var data = new float[] { 0f, 100f, 50f, 75f };
        var provider = new RawImageData<float>(2, 2, data, "FLT_FMT");

        var vm = new ImageViewModel();
        vm.RawData = provider;

        // values should match original, so command is disabled
        Assert.That(vm.ResetMinMaxCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void RestorePalette_SelectsMatchingPaletteByName()
    {
        var vm = new ImageViewModel();
        var target = vm.Palettes[2];

        vm.RestorePalette(target.Name);

        Assert.That(vm.SelectedPalette.Name, Is.EqualTo(target.Name));
        Assert.That(vm.SelectedPaletteName, Is.EqualTo(target.Name));
    }

    [Test]
    public void RestorePalette_LeavesSelectionUnchanged_ForUnknownOrNullName()
    {
        var vm = new ImageViewModel();
        var original = vm.SelectedPalette.Name;

        vm.RestorePalette("does-not-exist");
        Assert.That(vm.SelectedPalette.Name, Is.EqualTo(original));

        vm.RestorePalette(null);
        Assert.That(vm.SelectedPalette.Name, Is.EqualTo(original));
    }

    [Test]
    public async Task OpenCommand_UsesInjectedImageLoader()
    {
        var provider = new RawImageData<int>(2, 2, new int[4], "FAKE_FMT");
        var loader = new FakeImageLoader(provider);
        var dialog = new SpyDialogService { OpenFilePath = "C:/fake/image.fake" };

        var vm = new ImageViewModel(dialog, [loader]);

        await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)vm.OpenCommand).ExecuteAsync(null);

        Assert.That(loader.LoadCount, Is.EqualTo(1));
        Assert.That(vm.RawData, Is.SameAs(provider));
        Assert.That(vm.Filename, Is.EqualTo("image.fake"));
    }

    private sealed class SpyDialogService : IDialogService
    {
        public bool ShutdownRequested { get; private set; }
        public bool AboutShown { get; private set; }
        public string? OpenFilePath { get; set; }
        public string? ShowOpenFileDialog(string filter) => OpenFilePath;
        public void ShowError(string title, string message) { }
        public void ShowAbout() => AboutShown = true;
        public void RequestShutdown() => ShutdownRequested = true;
    }

    private sealed class FakeImageLoader : IImageLoader
    {
        private readonly IRawImageDataProvider _provider;
        public int LoadCount { get; private set; }

        public FakeImageLoader(IRawImageDataProvider provider) => _provider = provider;

        public bool CanLoad(string extension) => extension.Equals(".fake", StringComparison.OrdinalIgnoreCase);

        public Task<IRawImageDataProvider> LoadImageAsync(string path)
        {
            LoadCount++;
            return Task.FromResult(_provider);
        }
    }
}
