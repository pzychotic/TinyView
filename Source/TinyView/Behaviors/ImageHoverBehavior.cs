using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace TinyView.Behaviors;

public sealed class ImageHoverBehavior : Behavior<Image>
{
    public static readonly DependencyProperty HoverCommandProperty =
        DependencyProperty.Register(
            nameof(HoverCommand),
            typeof(ICommand),
            typeof(ImageHoverBehavior),
            new PropertyMetadata(null));

    public ICommand? HoverCommand
    {
        get => (ICommand?)GetValue(HoverCommandProperty);
        set => SetValue(HoverCommandProperty, value);
    }

    public static readonly DependencyProperty LeaveCommandProperty =
        DependencyProperty.Register(
            nameof(LeaveCommand),
            typeof(ICommand),
            typeof(ImageHoverBehavior),
            new PropertyMetadata(null));

    public ICommand? LeaveCommand
    {
        get => (ICommand?)GetValue(LeaveCommandProperty);
        set => SetValue(LeaveCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseMove += OnMouseMove;
        AssociatedObject.MouseLeave += OnMouseLeave;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.MouseMove -= OnMouseMove;
        AssociatedObject.MouseLeave -= OnMouseLeave;
        base.OnDetaching();
    }

    // Tracks whether the cursor is currently over the bitmap, so LeaveCommand
    // fires once per inside→outside transition instead of on every MouseMove.
    private bool _isInsideImage;

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (HoverCommand == null) return;

        if (ImagePixelHelper.GetPixelPosition(AssociatedObject, e) is not var (pos, bmp))
            return;

        int x = (int)pos.X;
        int y = (int)pos.Y;

        if (x < 0 || x >= bmp.PixelWidth || y < 0 || y >= bmp.PixelHeight)
        {
            NotifyLeave();
            return;
        }

        _isInsideImage = true;
        var pixel = new ViewModels.PixelPosition(x, y);
        if (HoverCommand.CanExecute(pixel))
            HoverCommand.Execute(pixel);

        e.Handled = true;
    }

    private void OnMouseLeave(object? sender, MouseEventArgs e)
    {
        _isInsideImage = false;
        if (LeaveCommand != null && LeaveCommand.CanExecute(null))
            LeaveCommand.Execute(null);

        e.Handled = true;
    }

    private void NotifyLeave()
    {
        if (!_isInsideImage)
            return;

        _isInsideImage = false;
        if (LeaveCommand != null && LeaveCommand.CanExecute(null))
            LeaveCommand.Execute(null);
    }
}
