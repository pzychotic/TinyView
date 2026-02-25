using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Reflection;

namespace TinyView.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        public string AppIcon { get; } = "/Resources/AppIcon.png";
        public string AppName { get; }
        public string Version { get; }
        public string RepoUrl { get; } = "https://github.com/pzychotic/TinyView";

        public AboutViewModel()
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var ver = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            AppName = asm.GetName().Name ?? "TinyView";
            Version = ver?.Split('+')[0] ?? "0.0.0"; // strip '+sha' if present
        }

        [RelayCommand]
        private void OpenRepo() => Process.Start(new ProcessStartInfo(RepoUrl) { UseShellExecute = true });

        // close command is a no-op in the VM, the CloseOnCommand behavior will invoke it and then close the window
        [RelayCommand]
        private void Close() { }
    }
}
