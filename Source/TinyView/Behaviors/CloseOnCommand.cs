using CommunityToolkit.Mvvm.Input;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace TinyView.Behaviors
{
    /// <summary>
    /// Behavior that exposes a window-scoped proxy command which executes a target ICommand
    /// and then closes the window. The behavior also wires the Escape key to the proxy command.
    /// </summary>
    public class CloseOnCommand : Behavior<Window>
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(ICommand), typeof(CloseOnCommand), new PropertyMetadata(null, OnCommandChanged));

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        // proxy command exposed on the window so controls can bind to it
        public static readonly DependencyProperty ProxyCommandProperty =
            DependencyProperty.RegisterAttached("ProxyCommand", typeof(ICommand), typeof(CloseOnCommand), new PropertyMetadata(null));

        public static ICommand? GetProxyCommand(DependencyObject d) => (ICommand?)d.GetValue(ProxyCommandProperty);
        private static void SetProxyCommand(DependencyObject d, ICommand? value) => d.SetValue(ProxyCommandProperty, value);

        private KeyBinding? _escKeyBinding;

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not CloseOnCommand behavior)
                return;

            behavior.UpdateCommand(e.NewValue as ICommand);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            UpdateCommand(Command);
        }

        protected override void OnDetaching()
        {
            UpdateCommand(null);
            base.OnDetaching();
        }

        private void UpdateCommand(ICommand? target)
        {
            if (AssociatedObject == null)
                return;

            // remove previous key binding if any
            if (_escKeyBinding != null)
            {
                AssociatedObject.InputBindings.Remove(_escKeyBinding);
                _escKeyBinding = null;
            }

            if (target != null)
            {
                // lightweight proxy command
                var proxy = new RelayCommand<object?>(
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
                        _ = AssociatedObject.Dispatcher.InvokeAsync(AssociatedObject.Close);
                    },
                    canExecute: param => target.CanExecute(param)
                );

                SetProxyCommand(AssociatedObject, proxy);

                // wire Escape to the proxy command
                _escKeyBinding = new KeyBinding(proxy, new KeyGesture(Key.Escape));
                AssociatedObject.InputBindings.Add(_escKeyBinding);
            }
            else
            {
                SetProxyCommand(AssociatedObject, null);
            }
        }
    }
}
