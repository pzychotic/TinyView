using CommunityToolkit.Mvvm.ComponentModel;

namespace TinyView.ViewModels
{
    public partial class ZoomState : ObservableObject
    {
        public const double DefaultFactor = 1.0;
        public const double MinFactor = 1.0 / 64.0; // 1.5%
        public const double MaxFactor = 64.0;       // 6400%
        public const double ButtonStep = 2.0;

        private double _factor = DefaultFactor;
        public double Factor
        {
            get => _factor;
            set
            {
                var clamped = Math.Clamp(value, MinFactor, MaxFactor);
                if (SetProperty(ref _factor, clamped))
                {
                    OnPropertyChanged(nameof(CanZoomIn));
                    OnPropertyChanged(nameof(CanZoomOut));
                }
            }
        }

        public bool CanZoomIn => Factor < MaxFactor;
        public bool CanZoomOut => Factor > MinFactor;

        public void ZoomIn() => Factor *= ButtonStep;
        public void ZoomOut() => Factor /= ButtonStep;
        public void Reset() => Factor = DefaultFactor;
    }
}
