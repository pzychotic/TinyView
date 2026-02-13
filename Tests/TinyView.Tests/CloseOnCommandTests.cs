using System.Windows;
using System.Windows.Input;
using TinyView.Behaviors;

namespace TinyView.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class CloseOnCommandTests
    {
        [Test]
        public void SettingCommand_AttachesProxyAndEscKeyBinding()
        {
            var window = new Window();
            var cmd = new TestCommand();

            Assert.That(CloseOnCommand.GetProxyCommand(window), Is.Null);

            CloseOnCommand.SetCommand(window, cmd);

            var proxy = CloseOnCommand.GetProxyCommand(window);
            Assert.That(proxy, Is.Not.Null);

            var kb = window.InputBindings.OfType<KeyBinding>().FirstOrDefault(k => (k.Gesture as KeyGesture)?.Key == Key.Escape);
            Assert.That(kb, Is.Not.Null);
            Assert.That(kb?.Command, Is.SameAs(proxy));
        }

        [Test]
        public void ProxyCommand_ExecutesTarget_And_ClosesWindow()
        {
            var window = new Window();
            var cmd = new TestCommand();

            CloseOnCommand.SetCommand(window, cmd);

            var proxy = CloseOnCommand.GetProxyCommand(window);
            Assert.That(proxy, Is.Not.Null);

            // show window so Close has visible effect
            window.Show();
            Assert.That(window.IsVisible, Is.True);

            // execute proxy which should invoke target and then close the window
            proxy?.Execute(null);

            // allow dispatcher to process the close
            window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Background);

            Assert.That(cmd.Executed, Is.True);
            Assert.That(window.IsVisible, Is.False);
        }

        [Test]
        public void ClearingCommand_RemovesProxyAndKeyBinding()
        {
            var window = new Window();
            var cmd = new TestCommand();

            CloseOnCommand.SetCommand(window, cmd);
            Assert.That(CloseOnCommand.GetProxyCommand(window), Is.Not.Null);

            // now clear
            CloseOnCommand.SetCommand(window, null);

            Assert.That(CloseOnCommand.GetProxyCommand(window), Is.Null);
            var kb = window.InputBindings.OfType<KeyBinding>().FirstOrDefault(k => (k.Gesture as KeyGesture)?.Key == Key.Escape);
            Assert.That(kb, Is.Null);
        }
    }
}
