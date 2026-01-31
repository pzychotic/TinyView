using System.Windows;
using System.Windows.Input;

namespace TinyView.Behaviors
{
    public static class DropBehavior
    {
        public static readonly DependencyProperty DropCommandProperty =
            DependencyProperty.RegisterAttached(
                "DropCommand",
                typeof(ICommand),
                typeof(DropBehavior),
                new PropertyMetadata(null, OnDropCommandChanged));

        public static void SetDropCommand(DependencyObject element, ICommand? value) =>
            element.SetValue(DropCommandProperty, value);

        public static ICommand? GetDropCommand(DependencyObject element) =>
            (ICommand?)element.GetValue(DropCommandProperty);

        private static void OnDropCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement ui)
            {
                if (e.OldValue == null && e.NewValue != null)
                {
                    ui.AllowDrop = true;
                    ui.DragEnter += OnDragEnter;
                    ui.Drop += OnDrop;
                }
                else if (e.OldValue != null && e.NewValue == null)
                {
                    ui.AllowDrop = false;
                    ui.DragEnter -= OnDragEnter;
                    ui.Drop -= OnDrop;
                }
            }
        }

        private static void OnDragEnter(object? sender, DragEventArgs e)
        {
            if (sender is UIElement ui)
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effects = DragDropEffects.Copy;
                else
                    e.Effects = DragDropEffects.None;

                e.Handled = true;
            }
        }

        private static void OnDrop(object? sender, DragEventArgs e)
        {
            if (sender is DependencyObject d)
            {
                var command = GetDropCommand(d);
                if (command == null) return;

                if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
                {
                    if (command.CanExecute(files))
                        command.Execute(files);
                }

                e.Handled = true;
            }
        }
    }
}
