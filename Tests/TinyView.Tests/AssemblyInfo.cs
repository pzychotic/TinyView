using System.Threading;

// TinyIcon uses in-box WPF imaging (RenderTargetBitmap, BitmapFrame). Those types have thread
// affinity and need a single-threaded apartment, so run the whole test assembly on an STA thread.
[assembly: Apartment(ApartmentState.STA)]
