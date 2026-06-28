using ImageMagick;
using TinyView.Models;

namespace TinyView.Services;

public sealed class MagickImageLoader : IImageLoader
{
    public bool CanLoad(string extension) => extension.Equals(".png", StringComparison.OrdinalIgnoreCase);

    public Task<IRawImageDataProvider> LoadImageAsync(string path)
        => Task.Run<IRawImageDataProvider>(() =>
        {
            using var image = new MagickImage(path);

            // The loader only reads channel 0, so require a single grayscale channel.
            if (image.ChannelCount != 1 || image.ColorType != ColorType.Grayscale)
                throw new InvalidOperationException("Expected a 16-bit grayscale image.");

            uint width = image.Width;
            uint height = image.Height;

            // extract raw 16-bit data (bulk read – single channel, so one value per pixel)
            ushort[] pixelData;
            using (var pixels = image.GetPixels())
            {
                pixelData = pixels.GetArea(0, 0, width, height)
                    ?? throw new InvalidOperationException("Failed to read pixel data.");
            }

            string format = "Gray16 (ushort)";
            return new RawImageData<ushort>((int)width, (int)height, pixelData, format);
        });
}
