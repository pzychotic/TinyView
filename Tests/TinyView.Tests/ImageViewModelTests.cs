using TinyView.Models;
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
            vm.ZoomFactor = 1.0;

            vm.ZoomInCommand.Execute(null);
            Assert.That(vm.ZoomFactor, Is.EqualTo(2.0));

            vm.ZoomOutCommand.Execute(null);
            vm.ZoomOutCommand.Execute(null);
            Assert.That(vm.ZoomFactor, Is.EqualTo(0.5));

            vm.ZoomFactor = 4.0;
            vm.ZoomResetCommand.Execute(null);
            Assert.That(vm.ZoomFactor, Is.EqualTo(1.0));
        }

        [Test]
        public void HoverCommand_SetsValueText_ForInRangeAndOutOfRange()
        {
            int width = 2, height = 2;
            var data = new int[width, height];
            data[0, 0] = 0;
            data[1, 0] = 255;
            data[0, 1] = 128;
            data[1, 1] = 64;

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
            var data = new int[width, height];
            data[0, 0] = 0;
            data[1, 0] = 255;
            data[0, 1] = 128;
            data[1, 1] = 64;

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
            var data = new int[width, height];
            data[0, 0] = 7;
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
    }
}
