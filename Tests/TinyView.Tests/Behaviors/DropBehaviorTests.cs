using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;
using TinyView.Behaviors;
using TinyView.Tests.TestSupport;

namespace TinyView.Tests.Behaviors;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class DropBehaviorTests
{
    [Test]
    public void SetDropCommand_AttachesCommandAndEnablesAllowDrop()
    {
        var element = new Border();
        var cmd = new TestCommand();
        var behavior = new DropBehavior();

        Assert.That(element.AllowDrop, Is.False);

        Interaction.GetBehaviors(element).Add(behavior);
        behavior.Command = cmd;

        Assert.That(element.AllowDrop, Is.True);
        var returned = behavior.Command;
        Assert.That(returned, Is.SameAs(cmd));
    }

    [Test]
    public void ClearingDropCommand_DisablesAllowDropAndRemovesCommand()
    {
        var element = new Border();
        var cmd = new TestCommand();
        var behavior = new DropBehavior();
        Interaction.GetBehaviors(element).Add(behavior);

        behavior.Command = cmd;
        Assert.That(element.AllowDrop, Is.True);

        Interaction.GetBehaviors(element).Remove(behavior);

        Assert.That(element.AllowDrop, Is.False);
        var returned = behavior.Command;
        Assert.That(returned, Is.SameAs(cmd));
    }

    [Test]
    public void ReplacingDropCommand_UpdatesStoredCommand()
    {
        var element = new Border();
        var cmd1 = new TestCommand();
        var cmd2 = new TestCommand();
        var behavior = new DropBehavior();
        Interaction.GetBehaviors(element).Add(behavior);

        behavior.Command = cmd1;
        Assert.That(behavior.Command, Is.SameAs(cmd1));

        behavior.Command = cmd2;
        Assert.That(element.AllowDrop, Is.True);
        Assert.That(behavior.Command, Is.SameAs(cmd2));
    }
}
