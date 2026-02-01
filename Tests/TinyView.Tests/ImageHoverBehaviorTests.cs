using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TinyView.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ImageHoverBehaviorTests
    {
        [Test]
        public void HoverCommand_WithBitmapSource_ExecutesCommandOnMouseMove()
        {
            var img = new Image();

            // create 10x10 pixel bitmap
            int width = 10, height = 10;
            var pixels = new byte[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = 0xFF;
            var bmp = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixels, width);

            img.Source = bmp;

            // arrange so ActualWidth/Height are > 0
            img.Measure(new Size(100, 100));
            img.Arrange(new Rect(0, 0, 100, 100));
            img.UpdateLayout();

            var cmd = new TestCommand();
            Behaviors.ImageHoverBehavior.SetHoverCommand(img, cmd);

            var args = new MouseEventArgs(InputManager.Current.PrimaryMouseDevice, 0)
            {
                RoutedEvent = UIElement.MouseMoveEvent
            };

            img.RaiseEvent(args);

            Assert.That(cmd.Executed, Is.True);
            Assert.That(args.Handled, Is.True);
        }

        [Test]
        public void HoverCommand_WithNullSource_DoesNotExecuteCommand()
        {
            var img = new Image();

            // no Source set
            img.Measure(new Size(100, 100));
            img.Arrange(new Rect(0, 0, 100, 100));
            img.UpdateLayout();

            var cmd = new TestCommand();
            Behaviors.ImageHoverBehavior.SetHoverCommand(img, cmd);

            var args = new MouseEventArgs(InputManager.Current.PrimaryMouseDevice, 0)
            {
                RoutedEvent = UIElement.MouseMoveEvent
            };

            img.RaiseEvent(args);

            Assert.That(cmd.Executed, Is.False);
            Assert.That(args.Handled, Is.False);
        }

        [Test]
        public void LeaveCommand_ExecutesOnMouseLeave()
        {
            var img = new Image();

            var cmd = new TestCommand();
            Behaviors.ImageHoverBehavior.SetLeaveCommand(img, cmd);

            var args = new MouseEventArgs(InputManager.Current.PrimaryMouseDevice, 0)
            {
                RoutedEvent = UIElement.MouseLeaveEvent
            };

            img.RaiseEvent(args);

            Assert.That(cmd.Executed, Is.True);
            Assert.That(args.Handled, Is.True);
        }
    }
}
