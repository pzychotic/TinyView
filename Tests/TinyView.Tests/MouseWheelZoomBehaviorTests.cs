using Microsoft.Xaml.Behaviors;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TinyView.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MouseWheelZoomBehaviorTests
    {
        [Test]
        public void DefaultZoomFactor_IsOne()
        {
            var element = new Border();
            var behavior = new Behaviors.MouseWheelZoomBehavior();
            Interaction.GetBehaviors(element).Add(behavior);
            Assert.That(behavior.ZoomFactor, Is.EqualTo(1.0));
        }

        [Test]
        public void SetZoomFactor_Works()
        {
            var element = new Border();
            var behavior = new Behaviors.MouseWheelZoomBehavior();
            Interaction.GetBehaviors(element).Add(behavior);
            behavior.ZoomFactor = 2.5;
            Assert.That(behavior.ZoomFactor, Is.EqualTo(2.5));
        }

        [Test]
        public void OnPreviewMouseWheel_NoCtrl_DoesNothing()
        {
            var element = new Border();
            var behavior = new Behaviors.MouseWheelZoomBehavior();
            Interaction.GetBehaviors(element).Add(behavior);

            // get private _wheelDeltaAccum field
            var accumField = typeof(Behaviors.MouseWheelZoomBehavior).GetField("_wheelDeltaAccum", BindingFlags.NonPublic | BindingFlags.Instance)!;

            // initial accum should be 0
            Assert.That((double)accumField.GetValue(behavior)!, Is.EqualTo(0.0));

            // call private OnPreviewMouseWheel without Ctrl modifier
            var mi = typeof(Behaviors.MouseWheelZoomBehavior).GetMethod("OnPreviewMouseWheel", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var args = new MouseWheelEventArgs(InputManager.Current.PrimaryMouseDevice, 0, 120)
            {
                RoutedEvent = UIElement.PreviewMouseWheelEvent
            };

            mi.Invoke(behavior, [element, args]);

            // because Ctrl was not pressed, nothing should have changed
            Assert.That((double)accumField.GetValue(behavior)!, Is.EqualTo(0.0));
            Assert.That(behavior.ZoomFactor, Is.EqualTo(1.0));
            Assert.That(args.Handled, Is.False);
        }
    }
}
