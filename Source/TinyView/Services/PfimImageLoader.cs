using Pfim;
using System.Runtime.InteropServices;
using TinyView.Models;

namespace TinyView.Services
{
    public class PfimImageLoader : IImageLoader
    {
        public bool CanLoad(string extension) => extension.Equals(".dds");

        public Task<IRawImageDataProvider> LoadImageAsync(string path)
            => Task.Run<IRawImageDataProvider>(() =>
            {
                using var image = Pfimage.FromFile(path);

                if (image.Format != ImageFormat.R16f && image.Format != ImageFormat.R32f)
                    throw new InvalidOperationException("Expected a 16/32-bit grayscale image.");

                int width = image.Width;
                int height = image.Height;
                bool isHalf = image.Format == ImageFormat.R16f;

                // extract raw data
                var pixelData = new float[width * height];

                if (isHalf)
                {
                    var halfData = MemoryMarshal.Cast<byte, Half>(image.Data);
                    for (int i = 0; i < pixelData.Length; i++)
                    {
                        pixelData[i] = (float)halfData[i];
                    }
                }
                else
                {
                    var floatData = MemoryMarshal.Cast<byte, float>(image.Data);
                    floatData.CopyTo(pixelData);
                }

                string format = isHalf ? "R16F (half)" : "R32F (float)";
                return new RawImageData<float>(width, height, pixelData, format);
            });
    }
}
