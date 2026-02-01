using ImageMagick;
using TinyView.Models;

namespace TinyView.Services
{
    public class MagickImageLoader : IImageLoader
    {
        public bool CanLoad(string extension) => extension.Equals(".png");

        public IRawImageDataProvider LoadImage(string path)
        {
            using var image = new MagickImage(path);

            if (image.ChannelCount != 1 && image.ColorType != ColorType.Grayscale)
                throw new InvalidOperationException("Expected a 16-bit grayscale image.");

            int width = (int)image.Width;
            int height = (int)image.Height;

            // extract raw 16-bit data
            var pixelData = new ushort[width, height];

            using (var pixels = image.GetPixels())
            {
                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; ++x)
                    {
                        ushort value = pixels.GetPixel(x, y).GetChannel(0);
                        pixelData[x, y] = value;
                    }
                }
            }

            string format = "Gray16 (ushort)";
            return new RawImageData<ushort>(width, height, pixelData, format);
        }
    }
}
