using Pfim;
using System.Runtime.InteropServices;
using TinyView.Models;

namespace TinyView.Services;

public sealed class PfimImageLoader : IImageLoader
{
    public bool CanLoad(string extension) => extension.Equals(".dds", StringComparison.OrdinalIgnoreCase);

    public Task<IRawImageDataProvider> LoadImageAsync(string path)
        => Task.Run<IRawImageDataProvider>(() =>
        {
            using var image = Pfimage.FromFile(path);

            if (image.Format != ImageFormat.R16f && image.Format != ImageFormat.R32f)
                throw new InvalidOperationException("Expected a 16/32-bit grayscale image.");

            int width = image.Width;
            int height = image.Height;

            if (image.Format == ImageFormat.R32f)
            {
                var floatData = MemoryMarshal.Cast<byte, float>(image.Data).ToArray();
                return new RawImageData<float>(width, height, floatData, "R32F (float)");
            }
            else
            {
                var halfData = MemoryMarshal.Cast<byte, Half>(image.Data).ToArray();
                return new RawImageData<Half>(width, height, halfData, "R16F (half)");
            }
        });
}
