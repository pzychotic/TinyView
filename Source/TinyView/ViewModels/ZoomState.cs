using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;

namespace TinyView.ViewModels;

public partial class ZoomState : ObservableObject
{
    public const double DefaultFactor = 1.0;
    public const double MinFactor = 1.0 / 64.0; // 1.5%
    public const double MaxFactor = 64.0;       // 6400%
    public const double ButtonStep = 2.0;

    /// <summary>
    /// Transient viewport-relative anchor point for the next zoom operation.
    /// Set by <see cref="Behaviors.MouseWheelZoomBehavior"/> before it changes
    /// the factor; consumed (read and cleared) by
    /// <see cref="Behaviors.ZoomCompensationBehavior"/> when it adjusts scroll offsets.
    /// </summary>
    [ObservableProperty]
    private Point? _anchor;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanZoomIn))]
    [NotifyPropertyChangedFor(nameof(CanZoomOut))]
    private double _factor = DefaultFactor;

    partial void OnFactorChanged(double value)
    {
        // NaN must not persist: it would poison all subsequent zoom math,
        // so treat it as invalid and fall back to the default factor.
        var clamped = double.IsNaN(value) ? DefaultFactor : Math.Clamp(value, MinFactor, MaxFactor);
        if (!EqualityComparer<double>.Default.Equals(value, clamped))
            Factor = clamped;
    }

    public bool CanZoomIn => Factor < MaxFactor;
    public bool CanZoomOut => Factor > MinFactor;

    public void ZoomIn() => Factor *= ButtonStep;
    public void ZoomOut() => Factor /= ButtonStep;
    public void Reset() => Factor = DefaultFactor;
}
