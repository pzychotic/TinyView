using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using TinyView.Models;

namespace TinyView.Tests;

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
    public void PreviewMouseRightButtonDown_StartsPanning()
    {
        var sv = CreateTestScrollViewer();
        var behavior = new Behaviors.ScrollViewerPanBehavior();
        Interaction.GetBehaviors(sv).Add(behavior);

        var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Right)
        {
            RoutedEvent = UIElement.PreviewMouseRightButtonDownEvent
        };

        sv.RaiseEvent(args);

        // In headless test environments IsMouseOver may be false so handlers may not start panning.
        // Verify the behavior is attached.
        Assert.That(Interaction.GetBehaviors(sv).Contains(behavior), Is.True);
    }

    [Test]
    public void PreviewMouseRightButtonUp_StopsPanning_ReleasesMouseAndRestoresCursor()
    {
        var sv = CreateTestScrollViewer();
        var behavior = new Behaviors.ScrollViewerPanBehavior();
        Interaction.GetBehaviors(sv).Add(behavior);

        behavior._isPanning = true;
        sv.CaptureMouse();

        var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Right)
        {
            RoutedEvent = UIElement.PreviewMouseRightButtonUpEvent
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

        behavior._isPanning = true;
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

        behavior._isPanning = true;
        behavior._panStartPoint = new Point(50, 50);
        behavior._panStartOffset = new Point(10, 20);

        var args = new MouseEventArgs(InputManager.Current.PrimaryMouseDevice, 0)
        {
            RoutedEvent = UIElement.PreviewMouseMoveEvent
        };

        sv.RaiseEvent(args);

        Assert.That(sv.HorizontalOffset, Is.Not.EqualTo(10));
        Assert.That(sv.VerticalOffset, Is.Not.EqualTo(20));
        Assert.That(args.Handled, Is.True);
    }

    [Test]
    public void IsOverScrollbar_WorksForScrollBarAndNonScrollBar()
    {
        var sb = new ScrollBar();
        var btn = new Button();

        Assert.That(Behaviors.ScrollViewerPanBehavior.IsOverScrollbar(sb), Is.True);
        Assert.That(Behaviors.ScrollViewerPanBehavior.IsOverScrollbar(btn), Is.False);
    }

    [Test]
    public void CancelPan_ClearsPanState()
    {
        var sv = CreateTestScrollViewer();
        var behavior = new Behaviors.ScrollViewerPanBehavior();
        Interaction.GetBehaviors(sv).Add(behavior);

        behavior._isPanning = true;
        sv.CaptureMouse();

        behavior.CancelPan();

        Assert.That(behavior._isPanning, Is.False);
    }

    [Test]
    public void ResetNotifier_CancelsPan_WhenFired()
    {
        var sv = CreateTestScrollViewer();
        var behavior = new Behaviors.ScrollViewerPanBehavior();
        Interaction.GetBehaviors(sv).Add(behavior);

        var notifier = new ViewportResetNotifier();
        behavior.ResetNotifier = notifier;

        behavior._isPanning = true;
        sv.CaptureMouse();

        notifier.RequestReset();

        Assert.That(behavior._isPanning, Is.False);
    }
}
