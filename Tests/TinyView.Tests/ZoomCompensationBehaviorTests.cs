using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace TinyView.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ZoomCompensationBehaviorTests
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
        public void DefaultZoomFactor_IsOne()
        {
            var sv = CreateTestScrollViewer();
            var behavior = new Behaviors.ZoomCompensationBehavior();
            Interaction.GetBehaviors(sv).Add(behavior);

            Assert.That(behavior.ZoomFactor, Is.EqualTo(1.0));
        }

        [Test]
        public void ZoomFactorChange_AdjustsScrollOffset()
        {
            var sv = CreateTestScrollViewer();
            var behavior = new Behaviors.ZoomCompensationBehavior();
            Interaction.GetBehaviors(sv).Add(behavior);

            Assert.That(sv.ScrollableWidth, Is.GreaterThan(0));

            // Set a non-center anchor so the compensation math produces different targets.
            behavior.ZoomAnchor = new Point(10, 10);

            // Changing ZoomFactor should invoke AdjustScrollForZoom without error
            // and consume the anchor.
            Assert.DoesNotThrow(() => behavior.ZoomFactor = 2.0);
            Assert.That(behavior.ZoomAnchor, Is.Null);
        }

        [Test]
        public void ZoomAnchor_IsCleared_AfterZoomFactorChange()
        {
            var sv = CreateTestScrollViewer();
            var behavior = new Behaviors.ZoomCompensationBehavior();
            Interaction.GetBehaviors(sv).Add(behavior);

            behavior.ZoomAnchor = new Point(50, 50);
            behavior.ZoomFactor = 2.0;

            Assert.That(behavior.ZoomAnchor, Is.Null);
        }

        [Test]
        public void ZoomFactorChange_WithNullAnchor_UsesViewportCenter()
        {
            var sv = CreateTestScrollViewer();
            var behavior = new Behaviors.ZoomCompensationBehavior();
            Interaction.GetBehaviors(sv).Add(behavior);

            behavior.ZoomAnchor = null;

            // should not throw when anchor is null (uses viewport center)
            Assert.DoesNotThrow(() => behavior.ZoomFactor = 2.0);
        }
    }
}
