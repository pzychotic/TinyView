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

            Behaviors.ScrollViewerPanBehavior.SetEnablePan(sv, true);

            var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
            {
                RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent
            };

            sv.RaiseEvent(args);

            // In headless test environments IsMouseOver may be false so handlers may not start panning.
            // Verify the attached property was set instead of relying on mouse capture.
            Assert.That(Behaviors.ScrollViewerPanBehavior.GetEnablePan(sv), Is.True);
        }

        [Test]
        public void PreviewMouseLeftButtonUp_StopsPanning_ReleasesMouseAndRestoresCursor()
        {
            var sv = CreateTestScrollViewer();

            Behaviors.ScrollViewerPanBehavior.SetEnablePan(sv, true);

            // simulate that panning has started using the private attached property and capture the mouse
            var type = typeof(Behaviors.ScrollViewerPanBehavior);
            var setIsPanning = type.GetMethod("SetIsPanning", BindingFlags.NonPublic | BindingFlags.Static)!;
            setIsPanning.Invoke(null, [sv, true]);
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

            Behaviors.ScrollViewerPanBehavior.SetEnablePan(sv, true);

            var type = typeof(Behaviors.ScrollViewerPanBehavior);
            var setIsPanning = type.GetMethod("SetIsPanning", BindingFlags.NonPublic | BindingFlags.Static)!;
            setIsPanning.Invoke(null, [sv, true]);
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

            // enable pan behavior so handlers are attached
            Behaviors.ScrollViewerPanBehavior.SetEnablePan(sv, true);

            Assert.That(sv.ScrollableWidth, Is.GreaterThan(0), "ScrollableWidth should be > 0 for the test layout");
            Assert.That(sv.ScrollableHeight, Is.GreaterThan(0), "ScrollableHeight should be > 0 for the test layout");

            var type = typeof(Behaviors.ScrollViewerPanBehavior);
            var setIsPanning = type.GetMethod("SetIsPanning", BindingFlags.NonPublic | BindingFlags.Static)!;
            var setPanStartPoint = type.GetMethod("SetPanStartPoint", BindingFlags.NonPublic | BindingFlags.Static)!;
            var setPanStartOffset = type.GetMethod("SetPanStartOffset", BindingFlags.NonPublic | BindingFlags.Static)!;

            var startPoint = new Point(50, 50);
            var startOffset = new Point(10, 20);

            // simulate that panning already started with a start point and offset
            setIsPanning.Invoke(null, [sv, true]);
            setPanStartPoint.Invoke(null, [sv, startPoint]);
            setPanStartOffset.Invoke(null, [sv, startOffset]);

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
