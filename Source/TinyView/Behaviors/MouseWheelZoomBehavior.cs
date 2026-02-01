using System;
using System.Windows;
using System.Windows.Input;

namespace TinyView.Behaviors
{
    public static class MouseWheelZoomBehavior
    {
        private const int WheelDeltaPerNotch = 120; // Win32 WHEEL_DELTA
        private const double DefaultZoomStep = 1.1; // 10% per notch

        public static readonly DependencyProperty EnableWheelZoomProperty =
            DependencyProperty.RegisterAttached(
                "EnableWheelZoom",
                typeof(bool),
                typeof(MouseWheelZoomBehavior),
                new PropertyMetadata(false, OnEnableChanged));

        public static void SetEnableWheelZoom(DependencyObject d, bool value) => d.SetValue(EnableWheelZoomProperty, value);
        public static bool GetEnableWheelZoom(DependencyObject d) => (bool)d.GetValue(EnableWheelZoomProperty);

        // two-way bindable ZoomFactor property
        public static readonly DependencyProperty ZoomFactorProperty =
            DependencyProperty.RegisterAttached(
                "ZoomFactor",
                typeof(double),
                typeof(MouseWheelZoomBehavior),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static void SetZoomFactor(DependencyObject d, double value) => d.SetValue(ZoomFactorProperty, value);
        public static double GetZoomFactor(DependencyObject d) => (double)d.GetValue(ZoomFactorProperty);

        // accumulator stored as attached property per element
        private static readonly DependencyProperty WheelDeltaAccumProperty =
            DependencyProperty.RegisterAttached(
                "WheelDeltaAccum",
                typeof(double),
                typeof(MouseWheelZoomBehavior),
                new PropertyMetadata(0.0));

        private static void SetWheelDeltaAccum(DependencyObject d, double value) => d.SetValue(WheelDeltaAccumProperty, value);
        private static double GetWheelDeltaAccum(DependencyObject d) => (double)d.GetValue(WheelDeltaAccumProperty);

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement ui)
            {
                if ((bool)e.NewValue)
                    ui.PreviewMouseWheel += OnPreviewMouseWheel;
                else
                    ui.PreviewMouseWheel -= OnPreviewMouseWheel;
            }
        }

        private static void OnPreviewMouseWheel(object? sender, MouseWheelEventArgs e)
        {
            if (sender is not DependencyObject d)
                return;

            // only zoom when Ctrl is held to allow normal scrolling otherwise
            if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                return;

            // accumulate delta to support high-resolution mice that send sub-notch values
            double deltaAccum = GetWheelDeltaAccum(d);
            deltaAccum += e.Delta;

            int wholeNotches = (int)(deltaAccum / WheelDeltaPerNotch);
            if (wholeNotches != 0)
            {
                double current = GetZoomFactor(d);
                current *= Math.Pow(DefaultZoomStep, wholeNotches);
                SetZoomFactor(d, current);

                // consume the notches we handled
                deltaAccum -= wholeNotches * WheelDeltaPerNotch;
            }

            SetWheelDeltaAccum(d, deltaAccum);

            e.Handled = true;
        }
    }
}