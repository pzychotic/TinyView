using Pfim;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TinyView
{
    public class PfimImageLoader
    {
        public static (IRawImageDataProvider, WriteableBitmap) LoadImage(string path)
        {
            using var image = Pfimage.FromFile(path);
                
            if (image.Format != ImageFormat.R16f && image.Format != ImageFormat.R32f)
                throw new InvalidOperationException("Expected a 16/32-bit grayscale image.");

            int width = image.Width;
            int height = image.Height;
            bool isHalf = image.Format == ImageFormat.R16f;

            // extract raw data
            var pixelData = new float[width, height];

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float value = isHalf ? ((float)BitConverter.ToHalf(image.Data, (y * width + x) * 2))
                                            : BitConverter.ToSingle(image.Data, (y * width + x) * 4);
                    pixelData[x, y] = value;
                }
            }

            var rawData = new RawImageData<float>(width, height, pixelData);

            // convert to 8-bit grayscale for display
            byte[] pixels8 = new byte[width * height];

            float min = pixelData.Cast<float>().Min();
            float max = pixelData.Cast<float>().Max();
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
