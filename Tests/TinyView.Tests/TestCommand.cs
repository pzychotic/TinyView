using System.Windows.Input;

namespace TinyView.Tests
{
    public class TestCommand : ICommand
    {
        public bool Executed { get; private set; }
        public bool CanExecuteReturn { get; set; } = true;

        public bool CanExecute(object? parameter) => CanExecuteReturn;
        public void Execute(object? parameter) => Executed = true;

#pragma warning disable CS0067 // The event is never used
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
    }
}
