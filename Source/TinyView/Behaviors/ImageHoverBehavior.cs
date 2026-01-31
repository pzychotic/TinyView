using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace TinyView.Behaviors
{
    public static class ImageHoverBehavior
    {
        public static readonly DependencyProperty HoverCommandProperty =
            DependencyProperty.RegisterAttached(
                "HoverCommand",
                typeof(ICommand),
                typeof(ImageHoverBehavior),
                new PropertyMetadata(null, OnHoverCommandChanged));

        public static void SetHoverCommand(DependencyObject element, ICommand? value) =>
            element.SetValue(HoverCommandProperty, value);

        public static ICommand? GetHoverCommand(DependencyObject element) =>
            (ICommand?)element.GetValue(HoverCommandProperty);

        public static readonly DependencyProperty LeaveCommandProperty =
            DependencyProperty.RegisterAttached(
                "LeaveCommand",
                typeof(ICommand),
                typeof(ImageHoverBehavior),
                new PropertyMetadata(null, OnLeaveCommandChanged));

        public static void SetLeaveCommand(DependencyObject element, ICommand? value) =>
            element.SetValue(LeaveCommandProperty, value);

        public static ICommand? GetLeaveCommand(DependencyObject element) =>
            (ICommand?)element.GetValue(LeaveCommandProperty);

        private static void OnHoverCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Image img)
            {
                if (e.OldValue == null && e.NewValue != null)
                    img.MouseMove += OnMouseMove;
                else if (e.OldValue != null && e.NewValue == null)
                    img.MouseMove -= OnMouseMove;
            }
        }

        private static void OnLeaveCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Image img)
            {
                if (e.OldValue == null && e.NewValue != null)
                    img.MouseLeave += OnMouseLeave;
                else if (e.OldValue != null && e.NewValue == null)
                    img.MouseLeave -= OnMouseLeave;
            }
        }

        private static void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is not Image img)
                return;

            var cmd = GetHoverCommand(img);
            if (cmd == null) return;

            if (img.Source is not BitmapSource bmp)
                return;

            double displayWidth = img.ActualWidth;
            double displayHeight = img.ActualHeight;
            if (displayWidth <= 0 || displayHeight <= 0)
                return;

            var pos = e.GetPosition(img);

            int x = (int)(pos.X * bmp.PixelWidth / displayWidth);
            int y = (int)(pos.Y * bmp.PixelHeight / displayHeight);

            if (x < 0 || x >= bmp.PixelWidth || y < 0 || y >= bmp.PixelHeight)
            {
                var leave = GetLeaveCommand(img);
                if (leave != null && leave.CanExecute(null))
                    leave.Execute(null);
                return;
            }

            var pixel = new ViewModels.PixelPosition(x, y);
            if (cmd.CanExecute(pixel))
                cmd.Execute(pixel);

            e.Handled = true;
        }

        private static void OnMouseLeave(object? sender, MouseEventArgs e)
        {
            if (sender is not Image img)
                return;

            var cmd = GetLeaveCommand(img);
            if (cmd != null && cmd.CanExecute(null))
                cmd.Execute(null);

            e.Handled = true;
        }
    }
}
