using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace TinyView.Behaviors
{
    public class MouseWheelZoomBehavior : Behavior<UIElement>
    {
        private const int WheelDeltaPerNotch = 120; // Win32 WHEEL_DELTA
        private const double DefaultZoomStep = 1.1; // 10% per notch

        // two-way bindable ZoomFactor property
        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.Register(
                nameof(ZoomFactor),
                typeof(double),
                typeof(MouseWheelZoomBehavior),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double ZoomFactor
        {
            get => (double)GetValue(ZoomFactorProperty);
            set => SetValue(ZoomFactorProperty, value);
        }

        private double _wheelDeltaAccum;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
            base.OnDetaching();
        }

        private void OnPreviewMouseWheel(object? sender, MouseWheelEventArgs e)
        {
            // only zoom when Ctrl is held to allow normal scrolling otherwise
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                return;

            // accumulate delta to support high-resolution mice that send sub-notch values
            _wheelDeltaAccum += e.Delta;

            int wholeNotches = (int)(_wheelDeltaAccum / WheelDeltaPerNotch);
            if (wholeNotches != 0)
            {
                double current = ZoomFactor;
                current *= Math.Pow(DefaultZoomStep, wholeNotches);
                ZoomFactor = current;

                // consume the notches we handled
                _wheelDeltaAccum -= wholeNotches * WheelDeltaPerNotch;
            }

            e.Handled = true;
        }
    }
}
