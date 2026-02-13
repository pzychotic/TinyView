using System.Windows;
using System.Windows.Input;
using TinyView.ViewModels;

namespace TinyView.Behaviors
{
    /// <summary>
    /// Attached behavior that exposes a window-scoped proxy command which executes a target ICommand
    /// and then closes the window. The behavior also wires the Escape key to the proxy command.
    /// Usage:
    ///   behaviors:CloseOnCommand.Command="{Binding CloseCommand}"
    /// Bind a control to the window proxy:
    ///   Command="{Binding Path=(behaviors:CloseOnCommand.ProxyCommand), RelativeSource={RelativeSource AncestorType=Window}}"
    /// </summary>
    public static class CloseOnCommand
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(CloseOnCommand), new PropertyMetadata(null, OnCommandChanged));

        public static void SetCommand(DependencyObject d, ICommand? value) => d.SetValue(CommandProperty, value);
        public static ICommand? GetCommand(DependencyObject d) => (ICommand?)d.GetValue(CommandProperty);

        // proxy command exposed on the window so controls can bind to it
        public static readonly DependencyProperty ProxyCommandProperty =
            DependencyProperty.RegisterAttached("ProxyCommand", typeof(ICommand), typeof(CloseOnCommand), new PropertyMetadata(null));

        public static ICommand? GetProxyCommand(DependencyObject d) => (ICommand?)d.GetValue(ProxyCommandProperty);
        private static void SetProxyCommand(DependencyObject d, ICommand? value) => d.SetValue(ProxyCommandProperty, value);

        // store Esc keybinding so we can remove it when command changes
        private static readonly DependencyProperty EscKeyBindingProperty =
            DependencyProperty.RegisterAttached("EscKeyBinding", typeof(KeyBinding), typeof(CloseOnCommand), new PropertyMetadata(null));

        private static void SetEscKeyBinding(DependencyObject d, KeyBinding? kb) => d.SetValue(EscKeyBindingProperty, kb);
        private static KeyBinding? GetEscKeyBinding(DependencyObject d) => (KeyBinding?)d.GetValue(EscKeyBindingProperty);

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Window window)
                return;

            // remove previous key binding if any
            var oldKb = GetEscKeyBinding(window);
            if (oldKb != null)
            {
                window.InputBindings.Remove(oldKb);
                SetEscKeyBinding(window, null);
            }

            if (e.NewValue is ICommand target)
            {
                // lightweight proxy command
                var proxy = new RelayCommand(
                    execute: param =>
                    {
                        try
                        {
                            if (target.CanExecute(param))
                                target.Execute(param);
                        }
                        catch
                        {
                            // preserve close semantics even if target throws
                        }

                        // close the window asynchronously on the UI thread
                        _ = window.Dispatcher.InvokeAsync(window.Close);
                    },
                    canExecute: param => target.CanExecute(param)
                );

                SetProxyCommand(window, proxy);

                // wire Escape to the proxy command
                var kb = new KeyBinding(proxy, new KeyGesture(Key.Escape));
                window.InputBindings.Add(kb);
                SetEscKeyBinding(window, kb);
            }
            else
            {
                SetProxyCommand(window, null);
            }
        }
    }
}
