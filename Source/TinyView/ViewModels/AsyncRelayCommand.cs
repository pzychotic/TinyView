using System.Windows.Input;

namespace TinyView.ViewModels
{
    /// <summary>
    /// Async relay command that supports async handlers returning Task.
    /// </summary>
    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<T?, Task> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;
            return _canExecute(AsT(parameter));
        }

        public async void Execute(object? parameter)
        {
            await _execute(AsT(parameter)).ConfigureAwait(false);
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        private static T? AsT(object? parameter)
        {
            if (parameter is T t) return t;
            if (parameter == null) return default;
            return (T)parameter!;
        }
    }

    public class AsyncRelayCommand : AsyncRelayCommand<object?>
    {
        public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
            : base(execute, canExecute)
        {
        }
    }
}
