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

            // create a WPF bitmap
            var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Indexed8, BitmapPalettes.Gray256);
            bitmap.WritePixels(new Int32Rect(0, 0, width, height), rawData.IndexedData, width, 0);

            return (rawData, bitmap);
        }
    }
}
