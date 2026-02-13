using System.Diagnostics;
using System.Reflection;
using System.Windows.Input;

namespace TinyView.ViewModels
{
    public class AboutViewModel
    {
        public string AppIcon { get; } = "/Resources/AppIcon.png";
        public string AppName { get; }
        public string Version { get; }
        public string RepoUrl { get; } = "https://github.com/pzychotic/TinyView";

        public ICommand OpenRepoCommand { get; }
        public ICommand CloseCommand { get; }

        public AboutViewModel()
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var ver = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            AppName = asm.GetName().Name ?? "TinyView";
            Version = ver?.Split('+')[0] ?? "0.0.0"; // strip '+sha' if present

            OpenRepoCommand = new RelayCommand<object?>(_ => Process.Start(new ProcessStartInfo(RepoUrl) { UseShellExecute = true }));

            // close command is a no-op in the VM, the CloseOnCommand behavior will invoke it and then close the window
            CloseCommand = new RelayCommand<object?>(_ => { });
        }
    }
}
