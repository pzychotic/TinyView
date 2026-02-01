using Pfim;
using TinyView.Models;

namespace TinyView
{
    public class PfimImageLoader
    {
        public static IRawImageDataProvider LoadImage(string path)
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

            string format = isHalf ? "R16F (half)" : "R32F (float)";
            return new RawImageData<float>(width, height, pixelData, format);
        }
    }
}
