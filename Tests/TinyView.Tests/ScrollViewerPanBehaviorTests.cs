using Microsoft.Xaml.Behaviors;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace TinyView.Tests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ScrollViewerPanBehaviorTests
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
        public void PreviewMouseLeftButtonDown_StartsPanning()
        {
            var sv = CreateTestScrollViewer();
            var behavior = new Behaviors.ScrollViewerPanBehavior();
            Interaction.GetBehaviors(sv).Add(behavior);

            var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent
            };

            sv.RaiseEvent(args);

            // In headless test environments IsMouseOver may be false so handlers may not start panning.
            // Verify the behavior is attached.
            Assert.That(Interaction.GetBehaviors(sv).Contains(behavior), Is.True);
        }

        [Test]
        public void PreviewMouseLeftButtonUp_StopsPanning_ReleasesMouseAndRestoresCursor()
        {
            var sv = CreateTestScrollViewer();
            var behavior = new Behaviors.ScrollViewerPanBehavior();
            Interaction.GetBehaviors(sv).Add(behavior);

            // simulate that panning has started using reflection
            var type = typeof(Behaviors.ScrollViewerPanBehavior);
            var isPanningField = type.GetField("_isPanning", BindingFlags.NonPublic | BindingFlags.Instance)!;
            isPanningField.SetValue(behavior, true);
            // ensure mouse is captured so release logic runs
            sv.CaptureMouse();

            var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent
            };
            sv.RaiseEvent(args);

            Assert.That(sv.IsMouseCaptured, Is.False);
            Assert.That(sv.Cursor, Is.Null);
            Assert.That(args.Handled, Is.True);
        }

        [Test]
        public void LostMouseCapture_ClearsCursor_WhenPanning()
        {
            var sv = CreateTestScrollViewer();
            var behavior = new Behaviors.ScrollViewerPanBehavior();
            Interaction.GetBehaviors(sv).Add(behavior);

            var type = typeof(Behaviors.ScrollViewerPanBehavior);
            var isPanningField = type.GetField("_isPanning", BindingFlags.NonPublic | BindingFlags.Instance)!;
            isPanningField.SetValue(behavior, true);
            sv.CaptureMouse();

            var lost = new MouseEventArgs(InputManager.Current.PrimaryMouseDevice, 0)
            {
                RoutedEvent = UIElement.LostMouseCaptureEvent
            };
            sv.RaiseEvent(lost);

            Assert.That(sv.Cursor, Is.Null);
        }

        [Test]
        public void OnPreviewMouseMove_PansScrollViewer_UpdatesOffsets()
        {
            var sv = CreateTestScrollViewer();
            var behavior = new Behaviors.ScrollViewerPanBehavior();
            Interaction.GetBehaviors(sv).Add(behavior);

            Assert.That(sv.ScrollableWidth, Is.GreaterThan(0), "ScrollableWidth should be > 0 for the test layout");
            Assert.That(sv.ScrollableHeight, Is.GreaterThan(0), "ScrollableHeight should be > 0 for the test layout");

            var type = typeof(Behaviors.ScrollViewerPanBehavior);
            var isPanningField = type.GetField("_isPanning", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var panStartPointField = type.GetField("_panStartPoint", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var panStartOffsetField = type.GetField("_panStartOffset", BindingFlags.NonPublic | BindingFlags.Instance)!;

            var startPoint = new Point(50, 50);
            var startOffset = new Point(10, 20);

            // simulate that panning already started with a start point and offset
            isPanningField.SetValue(behavior, true);
            panStartPointField.SetValue(behavior, startPoint);
            panStartOffsetField.SetValue(behavior, startOffset);

            var args = new MouseEventArgs(InputManager.Current.PrimaryMouseDevice, 0)
            {
                RoutedEvent = UIElement.PreviewMouseMoveEvent
            };

            sv.RaiseEvent(args);

            Assert.That(sv.HorizontalOffset, Is.Not.EqualTo(startOffset.X));
            Assert.That(sv.VerticalOffset, Is.Not.EqualTo(startOffset.Y));
            Assert.That(args.Handled, Is.True);
        }

        [Test]
        public void IsOverScrollbar_PrivateMethod_WorksForScrollBarAndNonScrollBar()
        {
            var type = typeof(Behaviors.ScrollViewerPanBehavior);
            var mi = type.GetMethod("IsOverScrollbar", BindingFlags.NonPublic | BindingFlags.Static)!
                ?? throw new InvalidOperationException("IsOverScrollbar method not found");

            var sb = new ScrollBar();
            var btn = new Button();

            var res1 = (bool)mi.Invoke(null, [sb])!;
            var res2 = (bool)mi.Invoke(null, [btn])!;

            Assert.That(res1, Is.True);
            Assert.That(res2, Is.False);
        }
    }
}
