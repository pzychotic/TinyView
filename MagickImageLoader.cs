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

            // create a WPF bitmap
            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Indexed8, BitmapPalettes.Gray256);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), rawData.IndexedData, width, 0);

            return (rawData, bitmap);
        }
    }
}
