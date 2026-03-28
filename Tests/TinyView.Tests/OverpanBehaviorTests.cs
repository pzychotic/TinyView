using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using TinyView.Models;

namespace TinyView.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class OverpanBehaviorTests
    {
        private static ScrollViewer CreateTestScrollViewer()
        {
            var sv = new ScrollViewer();
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            sv.Content = new Border { Width = 500, Height = 500 };
            sv.Width = 100;
            sv.Height = 100;
            sv.Measure(new Size(100, 100));
            sv.Arrange(new Rect(0, 0, 100, 100));
            sv.UpdateLayout();

            return sv;
        }

        [Test]
        public void UpdateMargin_AppliesViewportMargin_WhenEnabled()
        {
            var sv = CreateTestScrollViewer();
            var behavior = new Behaviors.OverpanBehavior();
            Interaction.GetBehaviors(sv).Add(behavior);

            behavior.IsEnabled = true;
            behavior.UpdateMargin();
            sv.UpdateLayout();

            var wrapper = sv.Content as FrameworkElement;
            Assert.That(wrapper, Is.Not.Null);

            double vw = sv.ViewportWidth;
            double vh = sv.ViewportHeight;
            Assert.That(wrapper!.Margin.Left, Is.EqualTo(vw));
            Assert.That(wrapper.Margin.Top, Is.EqualTo(vh));
            Assert.That(wrapper.Margin.Right, Is.EqualTo(vw));
            Assert.That(wrapper.Margin.Bottom, Is.EqualTo(vh));
        }

        [Test]
        public void UpdateMargin_SetsZeroMargin_WhenDisabled()
        {
            var sv = CreateTestScrollViewer();
            var behavior = new Behaviors.OverpanBehavior();
            Interaction.GetBehaviors(sv).Add(behavior);

            behavior.IsEnabled = true;
            behavior.UpdateMargin();
            sv.UpdateLayout();

            behavior.IsEnabled = false;
            behavior.UpdateMargin();
            sv.UpdateLayout();

            var wrapper = sv.Content as FrameworkElement;
            Assert.That(wrapper, Is.Not.Null);
            Assert.That(wrapper!.Margin, Is.EqualTo(new Thickness(0)));
        }

        [Test]
        public void CenterContent_ScrollsToCenterOfScrollableArea()
        {
            var sv = CreateTestScrollViewer();
            var behavior = new Behaviors.OverpanBehavior();
            Interaction.GetBehaviors(sv).Add(behavior);

            Assert.That(sv.ScrollableWidth, Is.GreaterThan(0));
            Assert.That(sv.ScrollableHeight, Is.GreaterThan(0));

            behavior.CenterContent();
            sv.UpdateLayout();

            Assert.That(sv.HorizontalOffset, Is.EqualTo(sv.ScrollableWidth / 2.0));
            Assert.That(sv.VerticalOffset, Is.EqualTo(sv.ScrollableHeight / 2.0));
        }

        [Test]
        public void ResetNotifier_ResetsToCenter_WhenFired()
        {
            var sv = CreateTestScrollViewer();
            var behavior = new Behaviors.OverpanBehavior();
            Interaction.GetBehaviors(sv).Add(behavior);
            behavior.IsEnabled = false;

            var notifier = new ViewportResetNotifier();
            behavior.ResetNotifier = notifier;

            sv.ScrollToHorizontalOffset(50);
            sv.UpdateLayout();

            notifier.RequestReset();
            sv.UpdateLayout();

            // When overpan is disabled, ResetToCenter scrolls to 0,0.
            Assert.That(sv.HorizontalOffset, Is.EqualTo(0));
            Assert.That(sv.VerticalOffset, Is.EqualTo(0));
        }
    }
}
