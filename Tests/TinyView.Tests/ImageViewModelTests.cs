using TinyView.Models;
using TinyView.Services;
using TinyView.ViewModels;

namespace TinyView.Tests
{
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
        public void PanResetTrigger_TogglesWhenRawDataChanges()
        {
            var vm = new ImageViewModel();
            bool initial = vm.PanResetTrigger;

            vm.RawData = new RawImageData<int>(1, 1, new int[1], "INT_FMT");
            Assert.That(vm.PanResetTrigger, Is.Not.EqualTo(initial));
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

        private sealed class SpyDialogService : IDialogService
        {
            public bool ShutdownRequested { get; private set; }
            public bool AboutShown { get; private set; }
            public string? ShowOpenFileDialog(string filter) => null;
            public void ShowError(string title, string message) { }
            public void ShowAbout() => AboutShown = true;
            public void RequestShutdown() => ShutdownRequested = true;
        }
    }
}
