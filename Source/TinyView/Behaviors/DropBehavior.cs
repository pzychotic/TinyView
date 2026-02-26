using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace TinyView.Behaviors
{
    public class DropBehavior : Behavior<UIElement>
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
            AssociatedObject.DragEnter += OnDragEnter;
            AssociatedObject.Drop += OnDrop;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.AllowDrop = false;
            AssociatedObject.DragEnter -= OnDragEnter;
            AssociatedObject.Drop -= OnDrop;
            base.OnDetaching();
        }

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void OnDrop(object? sender, DragEventArgs e)
        {
            if (Command == null) return;

            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                if (Command.CanExecute(files))
                    Command.Execute(files);
            }

            e.Handled = true;
        }
    }
}
