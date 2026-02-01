using System.Windows.Input;

namespace TinyView.Tests
{
    public class TestCommand : ICommand
    {
        public bool Executed { get; private set; }
        public bool CanExecuteReturn { get; set; } = true;

        public bool CanExecute(object? parameter) => CanExecuteReturn;
        public void Execute(object? parameter) => Executed = true;

        public event EventHandler? CanExecuteChanged;
    }
}
