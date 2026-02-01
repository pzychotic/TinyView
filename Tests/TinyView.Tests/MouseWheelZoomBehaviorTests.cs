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
            Assert.That(Behaviors.MouseWheelZoomBehavior.GetZoomFactor(element), Is.EqualTo(1.0));
        }

        [Test]
        public void SetZoomFactor_Works()
        {
            var element = new Border();
            Behaviors.MouseWheelZoomBehavior.SetZoomFactor(element, 2.5);
            Assert.That(Behaviors.MouseWheelZoomBehavior.GetZoomFactor(element), Is.EqualTo(2.5));
        }

        [Test]
        public void EnableWheelZoom_SetGet()
        {
            var element = new Border();
            Behaviors.MouseWheelZoomBehavior.SetEnableWheelZoom(element, true);
            Assert.That(Behaviors.MouseWheelZoomBehavior.GetEnableWheelZoom(element), Is.True);
            Behaviors.MouseWheelZoomBehavior.SetEnableWheelZoom(element, false);
            Assert.That(Behaviors.MouseWheelZoomBehavior.GetEnableWheelZoom(element), Is.False);
        }

        [Test]
        public void OnPreviewMouseWheel_NoCtrl_DoesNothing()
        {
            var element = new Border();

            // get private WheelDeltaAccumProperty field
            var dpField = typeof(Behaviors.MouseWheelZoomBehavior).GetField("WheelDeltaAccumProperty", BindingFlags.NonPublic | BindingFlags.Static)!;
            var dp = (DependencyProperty)dpField.GetValue(null)!;

            // initial accum should be 0
            Assert.That((double)element.GetValue(dp), Is.EqualTo(0.0));

            // call private OnPreviewMouseWheel without Ctrl modifier
            var mi = typeof(Behaviors.MouseWheelZoomBehavior).GetMethod("OnPreviewMouseWheel", BindingFlags.NonPublic | BindingFlags.Static)!;
            var args = new MouseWheelEventArgs(InputManager.Current.PrimaryMouseDevice, 0, 120)
            {
                RoutedEvent = UIElement.PreviewMouseWheelEvent
            };

            mi.Invoke(null, [element, args]);

            // because Ctrl was not pressed, nothing should have changed
            Assert.That((double)element.GetValue(dp), Is.EqualTo(0.0));
            Assert.That(Behaviors.MouseWheelZoomBehavior.GetZoomFactor(element), Is.EqualTo(1.0));
            Assert.That(args.Handled, Is.False);
        }
    }
}
