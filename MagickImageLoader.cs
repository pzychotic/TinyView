using ImageMagick;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TinyView
{
    public class MagickImageLoader
    {
        public static (IRawImageDataProvider, WriteableBitmap) LoadImage(string path)
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

            var rawData = new RawImageData<ushort>(width, height, pixelData);

            // convert to 8-bit grayscale for display
            byte[] pixels8 = new byte[width * height];

            ushort min = pixelData.Cast<ushort>().Min();
            ushort max = pixelData.Cast<ushort>().Max();
            float scale = min == max ? 1f : 255f / (max - min);

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float norm = (pixelData[x, y] - min) * scale;
                    byte scaled = (byte)Math.Clamp(norm, 0, 255);
                    pixels8[y * width + x] = scaled;
                }
            }

            // create a WPF bitmap
            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels8, width, 0);

            return (rawData, bitmap);
        }
    }
}
