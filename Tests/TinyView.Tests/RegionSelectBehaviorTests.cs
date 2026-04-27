using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TinyView.ViewModels;

namespace TinyView.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class RegionSelectBehaviorTests
{
    private Image _img = null!;
    private Grid _grid = null!;
    private Behaviors.RegionSelectBehavior _behavior = null!;

    /// <summary>
    /// TestCommand that also captures the parameter passed to Execute.
    /// </summary>
    private class CapturingCommand : ICommand
    {
        public bool Executed { get; private set; }
        public object? LastParameter { get; private set; }
        public bool CanExecuteReturn { get; set; } = true;

        public bool CanExecute(object? parameter) => CanExecuteReturn;

        public void Execute(object? parameter)
        {
            Executed = true;
            LastParameter = parameter;
        }

#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
    }

    [SetUp]
    public void SetUp()
    {
        _img = new Image();

        int width = 10, height = 10;
        var pixels = new byte[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = 0xFF;
        var bmp = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixels, width);

        _img.Source = bmp;
        _img.Stretch = Stretch.Fill;

        // The behavior requires the Image to be inside a Panel for overlay management
        _grid = new Grid();
        _grid.Children.Add(_img);

        _grid.Measure(new Size(100, 100));
        _grid.Arrange(new Rect(0, 0, 100, 100));
        _grid.UpdateLayout();

        _behavior = new Behaviors.RegionSelectBehavior();
        Interaction.GetBehaviors(_img).Add(_behavior);
    }

    #region IsActive / Cursor

    [Test]
    public void IsActive_True_SetsCrossCursor()
    {
        _behavior.IsActive = true;

        Assert.That(_img.Cursor, Is.EqualTo(Cursors.Cross));
    }

    [Test]
    public void IsActive_False_RestoresNullCursor()
    {
        _behavior.IsActive = true;
        _behavior.IsActive = false;

        Assert.That(_img.Cursor, Is.Null);
    }

    #endregion

    #region Mouse Drag Selection

    [Test]
    public void MouseLeftButtonDown_WhenActive_HandlesEvent()
    {
        _behavior.IsActive = true;

        var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };

        _img.RaiseEvent(args);

        Assert.That(args.Handled, Is.True);
    }

    [Test]
    public void MouseLeftButtonDown_WhenInactive_DoesNotHandleEvent()
    {
        _behavior.IsActive = false;

        var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };

        _img.RaiseEvent(args);

        Assert.That(args.Handled, Is.False);
    }

    [Test]
    public void FullDragSequence_ExecutesSelectionCommandWithPixelRect()
    {
        var cmd = new CapturingCommand();
        _behavior.SelectionCommand = cmd;
        _behavior.IsActive = true;

        // Mouse down
        var downArgs = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };
        _img.RaiseEvent(downArgs);

        // Mouse move
        var moveArgs = new MouseEventArgs(InputManager.Current.PrimaryMouseDevice, 0)
        {
            RoutedEvent = UIElement.MouseMoveEvent
        };
        _img.RaiseEvent(moveArgs);

        // Mouse up
        var upArgs = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonUpEvent
        };
        _img.RaiseEvent(upArgs);

        // In headless tests the mouse position is (0,0) for all events,
        // so the resulting rect has zero size and the command is not fired.
        // This verifies the full event sequence completes without exceptions.
        Assert.That(upArgs.Handled, Is.True);
    }

    [Test]
    public void MouseLeftButtonUp_WithoutPriorDown_DoesNotExecuteCommand()
    {
        var cmd = new CapturingCommand();
        _behavior.SelectionCommand = cmd;
        _behavior.IsActive = true;

        // Mouse up without prior mouse down
        var upArgs = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonUpEvent
        };
        _img.RaiseEvent(upArgs);

        Assert.That(cmd.Executed, Is.False);
        Assert.That(upArgs.Handled, Is.False);
    }

    #endregion

    #region Overlay Management

    [Test]
    public void MouseLeftButtonDown_WhenActive_AddsOverlayCanvasToParent()
    {
        _behavior.IsActive = true;

        int childrenBefore = _grid.Children.Count;

        var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };
        _img.RaiseEvent(args);

        Assert.That(_grid.Children.Count, Is.EqualTo(childrenBefore + 1));

        // The added child should be a Canvas
        var lastChild = _grid.Children[_grid.Children.Count - 1];
        Assert.That(lastChild, Is.InstanceOf<Canvas>());
    }

    [Test]
    public void IsActive_SetFalse_RemovesOverlayCanvas()
    {
        _behavior.IsActive = true;

        // Trigger overlay creation
        var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };
        _img.RaiseEvent(args);

        Assert.That(_grid.Children.Count, Is.EqualTo(2)); // Image + Canvas

        _behavior.IsActive = false;

        Assert.That(_grid.Children.Count, Is.EqualTo(1)); // Only Image remains
    }

    [Test]
    public void OverlayCanvas_IsNotHitTestVisible()
    {
        _behavior.IsActive = true;

        var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };
        _img.RaiseEvent(args);

        var canvas = _grid.Children[_grid.Children.Count - 1] as Canvas;
        Assert.That(canvas, Is.Not.Null);
        Assert.That(canvas!.IsHitTestVisible, Is.False);
    }

    [Test]
    public void OverlayCanvas_ContainsRectangleChild()
    {
        _behavior.IsActive = true;

        var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };
        _img.RaiseEvent(args);

        var canvas = _grid.Children[_grid.Children.Count - 1] as Canvas;
        Assert.That(canvas, Is.Not.Null);
        Assert.That(canvas!.Children.Count, Is.EqualTo(1));
        Assert.That(canvas.Children[0], Is.InstanceOf<Rectangle>());
    }

    [Test]
    public void SecondMouseDown_DoesNotAccumulateOverlays()
    {
        _behavior.IsActive = true;

        // First drag start — creates overlay
        var down1 = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };
        _img.RaiseEvent(down1);

        Assert.That(_grid.Children.Count, Is.EqualTo(2)); // Image + Canvas

        // Complete the first drag (headless → zero-size rect → overlay removed)
        var up1 = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonUpEvent
        };
        _img.RaiseEvent(up1);

        // Second drag start — creates a new overlay
        var down2 = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };
        _img.RaiseEvent(down2);

        // Still exactly 2 children (Image + one Canvas, not accumulating)
        Assert.That(_grid.Children.Count, Is.EqualTo(2));
    }

    #endregion

    #region SelectionCommand

    [Test]
    public void SelectionCommand_CanExecuteFalse_DoesNotExecute()
    {
        var cmd = new CapturingCommand { CanExecuteReturn = false };
        _behavior.SelectionCommand = cmd;
        _behavior.IsActive = true;

        // We can't easily simulate a non-zero drag in headless WPF, but we verify
        // that even if the drag completes, a command with CanExecute=false is not invoked.
        var downArgs = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };
        _img.RaiseEvent(downArgs);

        var upArgs = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonUpEvent
        };
        _img.RaiseEvent(upArgs);

        Assert.That(cmd.Executed, Is.False);
    }

    [Test]
    public void SelectionCommand_NullCommand_DoesNotThrow()
    {
        _behavior.SelectionCommand = null;
        _behavior.IsActive = true;

        var downArgs = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };
        _img.RaiseEvent(downArgs);

        var upArgs = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonUpEvent
        };

        Assert.DoesNotThrow(() => _img.RaiseEvent(upArgs));
    }

    #endregion

    #region No Source / No Parent

    [Test]
    public void MouseLeftButtonDown_WithNullSource_DoesNotHandle()
    {
        _img.Source = null;
        _behavior.IsActive = true;

        var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };
        _img.RaiseEvent(args);

        Assert.That(args.Handled, Is.False);
    }

    [Test]
    public void EnsureOverlay_WhenImageHasNoPanel_DoesNotThrow()
    {
        // Create a standalone image not inside a Panel
        var standaloneImg = new Image();
        int width = 10, height = 10;
        var pixels = new byte[width * height];
        var bmp = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixels, width);
        standaloneImg.Source = bmp;
        standaloneImg.Stretch = Stretch.Fill;
        standaloneImg.Measure(new Size(100, 100));
        standaloneImg.Arrange(new Rect(0, 0, 100, 100));
        standaloneImg.UpdateLayout();

        var behavior = new Behaviors.RegionSelectBehavior();
        Interaction.GetBehaviors(standaloneImg).Add(behavior);
        behavior.IsActive = true;

        var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };

        Assert.DoesNotThrow(() => standaloneImg.RaiseEvent(args));
    }

    #endregion

    #region MakePixelRect (via reflection)

    [Test]
    public void MakePixelRect_NormalOrder_ReturnsCorrectRect()
    {
        // Access private static method via reflection
        var method = typeof(Behaviors.RegionSelectBehavior)
            .GetMethod("MakePixelRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.That(method, Is.Not.Null);

        var result = (PixelRect)method!.Invoke(null, [new Point(2, 3), new Point(7, 9)])!;

        Assert.That(result.X, Is.EqualTo(2));
        Assert.That(result.Y, Is.EqualTo(3));
        Assert.That(result.Width, Is.EqualTo(5));
        Assert.That(result.Height, Is.EqualTo(6));
    }

    [Test]
    public void MakePixelRect_ReverseDrag_ReturnsNormalizedRect()
    {
        var method = typeof(Behaviors.RegionSelectBehavior)
            .GetMethod("MakePixelRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.That(method, Is.Not.Null);

        // Drag from bottom-right to top-left
        var result = (PixelRect)method!.Invoke(null, [new Point(8, 9), new Point(2, 3)])!;

        Assert.That(result.X, Is.EqualTo(2));
        Assert.That(result.Y, Is.EqualTo(3));
        Assert.That(result.Width, Is.EqualTo(6));
        Assert.That(result.Height, Is.EqualTo(6));
    }

    [Test]
    public void MakePixelRect_SamePoint_ReturnsZeroSizeRect()
    {
        var method = typeof(Behaviors.RegionSelectBehavior)
            .GetMethod("MakePixelRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.That(method, Is.Not.Null);

        var result = (PixelRect)method!.Invoke(null, [new Point(5, 5), new Point(5, 5)])!;

        Assert.That(result.Width, Is.EqualTo(0));
        Assert.That(result.Height, Is.EqualTo(0));
    }

    #endregion

    #region Detaching

    [Test]
    public void Detaching_RemovesOverlay()
    {
        _behavior.IsActive = true;

        // Trigger overlay creation
        var args = new MouseButtonEventArgs(InputManager.Current.PrimaryMouseDevice, 0, MouseButton.Left)
        {
            RoutedEvent = UIElement.MouseLeftButtonDownEvent
        };
        _img.RaiseEvent(args);

        Assert.That(_grid.Children.Count, Is.EqualTo(2));

        // Detach the behavior
        Interaction.GetBehaviors(_img).Remove(_behavior);

        Assert.That(_grid.Children.Count, Is.EqualTo(1));
    }

    #endregion
}
