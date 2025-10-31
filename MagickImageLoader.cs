using ImageMagick;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TinyView
{
    public class MagickImageLoader
    {
        public static (ushort[,] rawData, WriteableBitmap bitmap) LoadImage(string path)
        {
            using var image = new MagickImage(path);

            if (image.ChannelCount != 1 && image.ColorType != ColorType.Grayscale)
                throw new InvalidOperationException("Expected a 16-bit grayscale image.");

            int width = (int)image.Width;
            int height = (int)image.Height;

            // extract raw 16-bit data
            ushort[,] rawData = new ushort[width, height];

            using (var pixels = image.GetPixels())
            {
                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; ++x)
                    {
                        ushort value = pixels.GetPixel(x, y).GetChannel(0);
                        rawData[x, y] = value;
                    }
                }
            }

            // convert to 8-bit grayscale for display
            byte[] pixels8 = new byte[width * height];

            ushort min = rawData.Cast<ushort>().Min();
            ushort max = rawData.Cast<ushort>().Max();
            float scale = min == max ? 1f : 255f / (max - min);

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float norm = (rawData[x, y] - min) * scale;
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
