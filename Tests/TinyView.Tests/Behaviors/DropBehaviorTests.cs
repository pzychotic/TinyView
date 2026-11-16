using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;
using TinyView.Behaviors;
using TinyView.Tests.TestSupport;

namespace TinyView.Tests.Behaviors;

[TestFixture]
public class DropBehaviorTests
{
    // The drop itself cannot be exercised here — DragEventArgs has no public constructor — so this
    // covers the attach/detach contract; the payload path is covered by MainViewModel's DropCommand tests.

    [Test]
    public void Command_DefaultsToNull()
    {
        var behavior = new DropBehavior();

        Assert.That(behavior.Command, Is.Null);
    }

    [Test]
    public void OnAttached_MakesTheElementADropTarget()
    {
        var element = new Border();
        var behavior = new DropBehavior();

        Interaction.GetBehaviors(element).Add(behavior);

        Assert.That(element.AllowDrop, Is.True);
    }

    [Test]
    public void OnDetaching_StopsAcceptingDrops()
    {
        var element = new Border();
        var behavior = new DropBehavior();
        var behaviors = Interaction.GetBehaviors(element);
        behaviors.Add(behavior);

        behaviors.Remove(behavior);

        Assert.That(element.AllowDrop, Is.False);
    }

    [Test]
    public void Command_CanBeReplacedWhileAttached()
    {
        var element = new Border();
        var behavior = new DropBehavior();
        Interaction.GetBehaviors(element).Add(behavior);

        var command = new TestCommand();
        behavior.Command = command;

        Assert.That(behavior.Command, Is.SameAs(command));
    }
}
