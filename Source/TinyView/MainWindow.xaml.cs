using System.ComponentModel;
using System.Windows;
using TinyView.ViewModels;

namespace TinyView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ImageViewModel _viewModel = new();

        public MainWindow()
        {
            InitializeComponent();

            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e?.PropertyName == nameof(ImageViewModel.RawData))
            {
                // ensure we're on UI thread
                Dispatcher.Invoke(() => _viewModel.ApplyPalette());
            }
        }
    }
}
