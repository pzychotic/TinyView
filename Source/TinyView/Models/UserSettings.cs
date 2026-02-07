namespace TinyView.Models
{
    public sealed class UserSettings
    {
        // Window size and position
        public double Width { get; set; } = 800;
        public double Height { get; set; } = 600;
        public double Left { get; set; } = 0;
        public double Top { get; set; } = 0;

        // Whether the window was maximized when the app exited
        public bool IsMaximized { get; set; } = false;
    }
}
