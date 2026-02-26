using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace TinyView.Behaviors
{
    public class ImageHoverBehavior : Behavior<Image>
    {
        public static readonly DependencyProperty HoverCommandProperty =
            DependencyProperty.Register(
                nameof(HoverCommand),
                typeof(ICommand),
                typeof(ImageHoverBehavior),
                new PropertyMetadata(null));

        public ICommand? HoverCommand
        {
            get => (ICommand?)GetValue(HoverCommandProperty);
            set => SetValue(HoverCommandProperty, value);
        }

        public static readonly DependencyProperty LeaveCommandProperty =
            DependencyProperty.Register(
                nameof(LeaveCommand),
                typeof(ICommand),
                typeof(ImageHoverBehavior),
                new PropertyMetadata(null));

        public ICommand? LeaveCommand
        {
            get => (ICommand?)GetValue(LeaveCommandProperty);
            set => SetValue(LeaveCommandProperty, value);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseMove += OnMouseMove;
            AssociatedObject.MouseLeave += OnMouseLeave;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseMove -= OnMouseMove;
            AssociatedObject.MouseLeave -= OnMouseLeave;
            base.OnDetaching();
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (HoverCommand == null) return;

            if (AssociatedObject.Source is not BitmapSource bmp)
                return;

            double displayWidth = AssociatedObject.ActualWidth;
            double displayHeight = AssociatedObject.ActualHeight;
            if (displayWidth <= 0 || displayHeight <= 0)
                return;

            var pos = e.GetPosition(AssociatedObject);

            int x = (int)(pos.X * bmp.PixelWidth / displayWidth);
            int y = (int)(pos.Y * bmp.PixelHeight / displayHeight);

            if (x < 0 || x >= bmp.PixelWidth || y < 0 || y >= bmp.PixelHeight)
            {
                if (LeaveCommand != null && LeaveCommand.CanExecute(null))
                    LeaveCommand.Execute(null);
                return;
            }

            var pixel = new ViewModels.PixelPosition(x, y);
            if (HoverCommand.CanExecute(pixel))
                HoverCommand.Execute(pixel);

            e.Handled = true;
        }

        private void OnMouseLeave(object? sender, MouseEventArgs e)
        {
            if (LeaveCommand != null && LeaveCommand.CanExecute(null))
                LeaveCommand.Execute(null);

            e.Handled = true;
        }
    }
}
