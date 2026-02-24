using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TinyView.ViewModels
{
    public class ZoomState : INotifyPropertyChanged
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
                if (clamped == _factor) return;
                _factor = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanZoomIn));
                OnPropertyChanged(nameof(CanZoomOut));
            }
        }

        public bool CanZoomIn => _factor < MaxFactor;
        public bool CanZoomOut => _factor > MinFactor;

        public void ZoomIn() => Factor *= ButtonStep;
        public void ZoomOut() => Factor /= ButtonStep;
        public void Reset() => Factor = DefaultFactor;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
