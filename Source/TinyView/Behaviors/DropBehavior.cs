using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace TinyView.Behaviors;

/// <summary>
/// Makes the associated element a file drop target and forwards the dropped paths to <see cref="Command"/>.
/// Which files are accepted is the command's business, not the behavior's.
/// </summary>
public sealed class DropBehavior : Behavior<UIElement>
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(DropBehavior),
            new PropertyMetadata(null));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.AllowDrop = true;
        AssociatedObject.DragEnter += OnDragOver;
        // Children of the drop target reset the effect as the pointer moves over them, so keep setting it.
        AssociatedObject.DragOver += OnDragOver;
        AssociatedObject.Drop += OnDrop;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.AllowDrop = false;
        AssociatedObject.DragEnter -= OnDragOver;
        AssociatedObject.DragOver -= OnDragOver;
        AssociatedObject.Drop -= OnDrop;
        base.OnDetaching();
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;

        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        if (Command is null)
            return;

        if (e.Data.GetData(DataFormats.FileDrop) is string[] files && Command.CanExecute(files))
            Command.Execute(files);

        e.Handled = true;
    }
}
