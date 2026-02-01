using System.Windows.Controls;

namespace TinyView.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class DropBehaviorTests
    {
        [Test]
        public void SetDropCommand_AttachesCommandAndEnablesAllowDrop()
        {
            var element = new Border();
            var cmd = new TestCommand();

            Assert.That(element.AllowDrop, Is.False);

            Behaviors.DropBehavior.SetDropCommand(element, cmd);

            Assert.That(element.AllowDrop, Is.True);
            var returned = Behaviors.DropBehavior.GetDropCommand(element);
            Assert.That(returned, Is.SameAs(cmd));
        }

        [Test]
        public void ClearingDropCommand_DisablesAllowDropAndRemovesCommand()
        {
            var element = new Border();
            var cmd = new TestCommand();

            Behaviors.DropBehavior.SetDropCommand(element, cmd);
            Assert.That(element.AllowDrop, Is.True);

            Behaviors.DropBehavior.SetDropCommand(element, null);

            Assert.That(element.AllowDrop, Is.False);
            var returned = Behaviors.DropBehavior.GetDropCommand(element);
            Assert.That(returned, Is.Null);
        }

        [Test]
        public void ReplacingDropCommand_UpdatesStoredCommand()
        {
            var element = new Border();
            var cmd1 = new TestCommand();
            var cmd2 = new TestCommand();

            Behaviors.DropBehavior.SetDropCommand(element, cmd1);
            Assert.That(Behaviors.DropBehavior.GetDropCommand(element), Is.SameAs(cmd1));

            Behaviors.DropBehavior.SetDropCommand(element, cmd2);
            Assert.That(element.AllowDrop, Is.True);
            Assert.That(Behaviors.DropBehavior.GetDropCommand(element), Is.SameAs(cmd2));
        }
    }
}
