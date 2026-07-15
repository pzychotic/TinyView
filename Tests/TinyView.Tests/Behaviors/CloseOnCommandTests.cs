using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;
using TinyView.Behaviors;
using TinyView.Tests.TestSupport;

namespace TinyView.Tests.Behaviors;

[TestFixture]
public class CloseOnCommandTests
{
    [Test]
    public void SettingCommand_AttachesProxyAndEscKeyBinding()
    {
        var window = new Window();
        var cmd = new TestCommand();
        var behavior = new CloseOnCommand();
        Interaction.GetBehaviors(window).Add(behavior);

        Assert.That(CloseOnCommand.GetProxyCommand(window), Is.Null);

        behavior.Command = cmd;

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
        var behavior = new CloseOnCommand();
        Interaction.GetBehaviors(window).Add(behavior);

        behavior.Command = cmd;

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
    public void ProxyCommand_ForwardsTargetCanExecuteChanged()
    {
        var window = new Window();
        var cmd = new TestCommand();
        var behavior = new CloseOnCommand();
        Interaction.GetBehaviors(window).Add(behavior);

        behavior.Command = cmd;

        var proxy = CloseOnCommand.GetProxyCommand(window);
        Assert.That(proxy, Is.Not.Null);
        Assert.That(proxy!.CanExecute(null), Is.True);

        bool raised = false;
        proxy.CanExecuteChanged += (_, _) => raised = true;

        cmd.CanExecuteReturn = false;
        cmd.RaiseCanExecuteChanged();

        Assert.Multiple(() =>
        {
            Assert.That(raised, Is.True, "proxy should re-raise the target's CanExecuteChanged");
            Assert.That(proxy.CanExecute(null), Is.False);
        });
    }

    [Test]
    public void ClearingCommand_StopsForwardingTheOldTargetsCanExecuteChanged()
    {
        var window = new Window();
        var cmd = new TestCommand();
        var behavior = new CloseOnCommand();
        Interaction.GetBehaviors(window).Add(behavior);

        behavior.Command = cmd;
        var proxy = CloseOnCommand.GetProxyCommand(window);
        Assert.That(proxy, Is.Not.Null);

        bool raised = false;
        proxy!.CanExecuteChanged += (_, _) => raised = true;

        behavior.Command = null;
        cmd.RaiseCanExecuteChanged();

        Assert.That(raised, Is.False, "a cleared command must not reach the stale proxy");
    }

    [Test]
    public void ClearingCommand_RemovesProxyAndKeyBinding()
    {
        var window = new Window();
        var cmd = new TestCommand();
        var behavior = new CloseOnCommand();
        Interaction.GetBehaviors(window).Add(behavior);

        behavior.Command = cmd;
        Assert.That(CloseOnCommand.GetProxyCommand(window), Is.Not.Null);

        // now clear
        behavior.Command = null;

        Assert.That(CloseOnCommand.GetProxyCommand(window), Is.Null);
        var kb = window.InputBindings.OfType<KeyBinding>().FirstOrDefault(k => (k.Gesture as KeyGesture)?.Key == Key.Escape);
        Assert.That(kb, Is.Null);
    }
}
