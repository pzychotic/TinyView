using System.Windows.Input;

namespace TinyView.ViewModels
{
    /// <summary>
    /// Strongly-typed RelayCommand implementation.
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (_canExecute == null) return true;
            return _canExecute(AsT(parameter));
        }

        public void Execute(object? parameter) => _execute(AsT(parameter));

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        private static T? AsT(object? parameter)
        {
            if (parameter is T t) return t;
            // handle null for reference and nullable value types
            if (parameter == null) return default;
            return (T)parameter!;
        }
    }

    /// <summary>
    /// Non-generic convenience alias for RelayCommand<object?> to preserve simple usage.
    /// </summary>
    public class RelayCommand : RelayCommand<object?>
    {
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
            : base(execute, canExecute)
        {
        }
    }
}
